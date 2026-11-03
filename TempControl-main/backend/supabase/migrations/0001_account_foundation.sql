-- Qscreen account foundation. Prepared against the inspected Supabase schema.
-- Not applied yet: the current MCP connection is read-only.
-- Intentionally does not provision an administrator or change Auth mail settings.
begin;

create schema qscreen_private;
revoke all on schema qscreen_private from public, anon, authenticated, service_role;
alter default privileges in schema qscreen_private revoke execute on functions from public;

create table qscreen_private.account_states (
    user_id uuid primary key references auth.users(id) on delete cascade,
    disabled boolean not null default false,
    created_at timestamptz not null default now()
);
create table qscreen_private.admin_identity (
    singleton boolean primary key default true check (singleton = true),
    user_id uuid not null unique references auth.users(id) on delete restrict,
    provisioned_at timestamptz not null default now()
);
create table qscreen_private.admin_audit_events (
    id uuid primary key default gen_random_uuid(),
    actor_id uuid,
    action text not null check (length(action) between 1 and 100),
    target_id uuid,
    occurred_at timestamptz not null default now()
);
create index qscreen_audit_time on qscreen_private.admin_audit_events(occurred_at desc);

create table public.qscreen_profiles (
    user_id uuid primary key references auth.users(id) on delete cascade,
    display_name text not null default 'Qscreen gebruiker'
        check (length(btrim(display_name)) between 1 and 120),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

alter table qscreen_private.account_states enable row level security;
alter table qscreen_private.admin_identity enable row level security;
alter table qscreen_private.admin_audit_events enable row level security;
alter table public.qscreen_profiles enable row level security;

revoke all on all tables in schema qscreen_private from public, anon, authenticated, service_role;
revoke all on public.qscreen_profiles from public, anon, authenticated, service_role;
grant usage on schema qscreen_private to authenticated;
grant select on public.qscreen_profiles to authenticated;
grant update (display_name) on public.qscreen_profiles to authenticated;

-- Security-definer helpers expose only caller-specific booleans, not Auth rows.
-- Their owner must remain the trusted migration role, never anon/authenticated.
create function qscreen_private.is_verified_user()
returns boolean language sql stable security definer set search_path = '' as $$
    select exists (
        select 1 from auth.users u
        join qscreen_private.account_states s on s.user_id = u.id
        where u.id = (select auth.uid())
          and u.email_confirmed_at is not null
          and u.deleted_at is null
          and (u.banned_until is null or u.banned_until <= now())
          and not s.disabled
    );
$$;
create function qscreen_private.is_admin()
returns boolean language sql stable security definer set search_path = '' as $$
    select qscreen_private.is_verified_user()
      and coalesce((select auth.jwt())->>'aal', '') = 'aal2'
      and exists (
          select 1 from qscreen_private.admin_identity a
          join auth.users u on u.id = a.user_id
          where a.user_id = (select auth.uid())
            and lower(u.email) = 'qprints@outlook.be'
            and exists (
                select 1 from auth.mfa_factors f
                where f.user_id = u.id and f.factor_type = 'totp'
                  and f.status = 'verified'
            )
      );
$$;
revoke all on function qscreen_private.is_verified_user() from public, anon, authenticated, service_role;
revoke all on function qscreen_private.is_admin() from public, anon, authenticated, service_role;
grant execute on function qscreen_private.is_verified_user() to authenticated;
grant execute on function qscreen_private.is_admin() to authenticated;

create policy qscreen_read_own_profile on public.qscreen_profiles
    for select to authenticated
    using (user_id = (select auth.uid()) and (select qscreen_private.is_verified_user()));
create policy qscreen_update_own_name on public.qscreen_profiles
    for update to authenticated
    using (user_id = (select auth.uid()) and (select qscreen_private.is_verified_user()))
    with check (user_id = (select auth.uid()) and (select qscreen_private.is_verified_user()));

create function qscreen_private.profile_updated_at()
returns trigger language plpgsql set search_path = '' as $$
begin
    new.updated_at := now();
    return new;
end;
$$;
revoke all on function qscreen_private.profile_updated_at() from public, anon, authenticated, service_role;
create trigger qscreen_profile_updated_at before update on public.qscreen_profiles
    for each row execute function qscreen_private.profile_updated_at();

-- User-controlled signup metadata can provide a name, never a role or status.
create function qscreen_private.on_user_created()
returns trigger language plpgsql security definer set search_path = '' as $$
begin
    insert into qscreen_private.account_states(user_id) values(new.id);
    insert into public.qscreen_profiles(user_id, display_name)
    values(new.id, left(coalesce(nullif(btrim(new.raw_user_meta_data->>'display_name'), ''), 'Qscreen gebruiker'), 120));
    return new;
end;
$$;
revoke all on function qscreen_private.on_user_created() from public, anon, authenticated, service_role;
create trigger qscreen_auth_user_created after insert on auth.users
    for each row execute function qscreen_private.on_user_created();

-- Safe backfill if a user was created between inspection and migration.
insert into qscreen_private.account_states(user_id)
    select id from auth.users on conflict (user_id) do nothing;
insert into public.qscreen_profiles(user_id, display_name)
    select id, left(coalesce(nullif(btrim(raw_user_meta_data->>'display_name'), ''), 'Qscreen gebruiker'),120)
    from auth.users on conflict (user_id) do nothing;

-- Read only the current identity; no password hashes or tokens are returned.
create function public.qscreen_account_status()
returns table(user_id uuid, display_name text, email_verified boolean, disabled boolean, account_role text)
language sql stable security definer set search_path = '' as $$
    select u.id, p.display_name, u.email_confirmed_at is not null,
           s.disabled or u.deleted_at is not null or coalesce(u.banned_until > now(), false),
           case when qscreen_private.is_admin() then 'admin'::text else 'user'::text end
    from auth.users u
    join public.qscreen_profiles p on p.user_id = u.id
    join qscreen_private.account_states s on s.user_id = u.id
    where u.id = (select auth.uid());
$$;
revoke all on function public.qscreen_account_status() from public, anon, authenticated, service_role;
grant execute on function public.qscreen_account_status() to authenticated;

-- Manual bootstrap only. No public API, registration flow or desktop key can call it.
-- This function is intentionally NOT invoked by this migration.
create function qscreen_private.provision_admin(verified_user_id uuid)
returns void language plpgsql security definer set search_path = '' as $$
begin
    if not exists (
        select 1 from auth.users u
        join qscreen_private.account_states s on s.user_id = u.id
        where u.id = verified_user_id
          and lower(u.email) = 'qprints@outlook.be'
          and u.email_confirmed_at is not null and u.deleted_at is null
          and (u.banned_until is null or u.banned_until <= now()) and not s.disabled
          and exists (select 1 from auth.mfa_factors f where f.user_id = u.id
              and f.factor_type = 'totp' and f.status = 'verified')
    ) then
        raise exception 'Administrator requires the confirmed owner identity, active account and verified TOTP';
    end if;
    insert into qscreen_private.admin_identity(singleton,user_id) values(true, verified_user_id);
    insert into qscreen_private.admin_audit_events(actor_id,action,target_id)
        values(verified_user_id,'admin.provisioned',verified_user_id);
end;
$$;
revoke all on function qscreen_private.provision_admin(uuid) from public, anon, authenticated, service_role;

comment on schema qscreen_private is 'Qscreen server-owned state: no client table grants or role updates.';
comment on function qscreen_private.provision_admin(uuid) is 'Trusted migration operator only; never invoke from registration or the desktop.';
commit;
