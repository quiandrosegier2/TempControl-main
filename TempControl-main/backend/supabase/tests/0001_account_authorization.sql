-- Database authorization tests, not end-to-end Auth/MFA verification.
-- Synthetic identities exist only inside this transaction. No email is sent.
-- Every inserted row, changed name and bootstrap operation is rolled back.
begin;

do $$ begin
    if exists(select 1 from auth.users where id in (
        'b028bc4e-8df0-4b29-959d-000000000001'::uuid,
        'b028bc4e-8df0-4b29-959d-000000000002'::uuid,
        'b028bc4e-8df0-4b29-959d-000000000003'::uuid,
        'b028bc4e-8df0-4b29-959d-000000000004'::uuid))
        or exists(select 1 from auth.users where lower(email)='qprints@outlook.be')
        or exists(select 1 from qscreen_private.admin_identity)
    then raise exception 'Test fixture would overlap real data; no tests executed'; end if;
end $$;

insert into auth.users(id,email,email_confirmed_at,raw_user_meta_data,aud,role) values
('b028bc4e-8df0-4b29-959d-000000000001','qscreen-test-a@example.invalid',now(),'{"display_name":"Test A","role":"admin","is_admin":true}','authenticated','authenticated'),
('b028bc4e-8df0-4b29-959d-000000000002','qscreen-test-b@example.invalid',now(),'{"display_name":"Test B"}','authenticated','authenticated'),
('b028bc4e-8df0-4b29-959d-000000000003','qscreen-unverified@example.invalid',null,'{}','authenticated','authenticated'),
('b028bc4e-8df0-4b29-959d-000000000004','Qprints@outlook.be',null,'{}','authenticated','authenticated');

-- Public registration metadata must not provision any administrator.
do $$ begin
    if (select count(*) from public.qscreen_profiles where user_id::text like 'b028bc4e-8df0-4b29-959d-%') <> 4 then raise exception 'Signup trigger missing profiles'; end if;
    if exists(select 1 from qscreen_private.admin_identity) then raise exception 'Registration granted admin'; end if;
    if (select count(*) from pg_class where oid in ('public.qscreen_profiles'::regclass,'qscreen_private.account_states'::regclass,'qscreen_private.admin_identity'::regclass,'qscreen_private.admin_audit_events'::regclass) and relrowsecurity) <> 4 then raise exception 'RLS missing'; end if;
end $$;

set local role anon;
do $$ begin
    begin perform * from public.qscreen_profiles;raise exception 'Anonymous profile read allowed';exception when insufficient_privilege then null;end;
    begin perform * from public.qscreen_account_status();raise exception 'Anonymous account RPC allowed';exception when insufficient_privilege then null;end;
end $$;
reset role;

select set_config('request.jwt.claims','{"sub":"b028bc4e-8df0-4b29-959d-000000000001","role":"authenticated","aal":"aal1"}',true);
set local role authenticated;
do $$ declare changed integer; begin
    if (select count(*) from public.qscreen_profiles) <> 1 then raise exception 'Cross-account profile read'; end if;
    if not exists(select 1 from public.qscreen_profiles where display_name='Test A') then raise exception 'Own profile missing'; end if;
    if (select account_role from public.qscreen_account_status()) <> 'user' then raise exception 'Metadata granted admin'; end if;
    update public.qscreen_profiles set display_name='Edited A' where user_id='b028bc4e-8df0-4b29-959d-000000000001';
    get diagnostics changed = row_count;if changed<>1 then raise exception 'Own display name update failed';end if;
    update public.qscreen_profiles set display_name='Unauthorized' where user_id='b028bc4e-8df0-4b29-959d-000000000002';
    get diagnostics changed = row_count;if changed<>0 then raise exception 'Cross-account profile update allowed';end if;
    begin update public.qscreen_profiles set user_id='b028bc4e-8df0-4b29-959d-000000000002';raise exception 'Ownership update allowed';exception when insufficient_privilege then null;end;
    begin update qscreen_private.account_states set disabled=false;raise exception 'Account-state update allowed';exception when insufficient_privilege then null;end;
    begin insert into qscreen_private.admin_identity(singleton,user_id) values(true,'b028bc4e-8df0-4b29-959d-000000000001');raise exception 'Direct admin insertion allowed';exception when insufficient_privilege then null;end;
    begin perform qscreen_private.provision_admin('b028bc4e-8df0-4b29-959d-000000000001');raise exception 'Public bootstrap allowed';exception when insufficient_privilege then null;end;
    begin insert into qscreen_private.admin_audit_events(action) values('forged');raise exception 'Audit forgery allowed';exception when insufficient_privilege then null;end;
end $$;
reset role;

select set_config('request.jwt.claims','{"sub":"b028bc4e-8df0-4b29-959d-000000000003","role":"authenticated","aal":"aal1"}',true);
set local role authenticated;
do $$ begin
    if exists(select 1 from public.qscreen_profiles) then raise exception 'Unverified user gained full profile access';end if;
    if (select email_verified from public.qscreen_account_status()) then raise exception 'Unverified account status incorrect';end if;
end $$;
reset role;

update qscreen_private.account_states set disabled=true where user_id='b028bc4e-8df0-4b29-959d-000000000001';
select set_config('request.jwt.claims','{"sub":"b028bc4e-8df0-4b29-959d-000000000001","role":"authenticated","aal":"aal1"}',true);
set local role authenticated;
do $$ begin
    if exists(select 1 from public.qscreen_profiles) then raise exception 'Disabled account retained profile access';end if;
    if not (select disabled from public.qscreen_account_status()) then raise exception 'Disabled status not reported';end if;
end $$;
reset role;

-- Verify the trusted bootstrap rejects incomplete ownership and missing MFA.
do $$ begin
    begin perform qscreen_private.provision_admin('b028bc4e-8df0-4b29-959d-000000000004');raise exception 'Unverified admin bootstrap allowed';
    exception when raise_exception then if sqlerrm not like 'Administrator requires%' then raise;end if;end;
end $$;
update auth.users set email_confirmed_at=now() where id='b028bc4e-8df0-4b29-959d-000000000004';
do $$ begin
    begin perform qscreen_private.provision_admin('b028bc4e-8df0-4b29-959d-000000000004');raise exception 'Admin bootstrap without MFA allowed';
    exception when raise_exception then if sqlerrm not like 'Administrator requires%' then raise;end if;end;
end $$;
insert into auth.mfa_factors(id,user_id,friendly_name,factor_type,status,secret,created_at,updated_at)
values('b028bc4e-8df0-4b29-959d-000000000005','b028bc4e-8df0-4b29-959d-000000000004','transaction-only-test','totp','verified','transaction-only-not-a-real-factor',now(),now());

select set_config('request.jwt.claims','{"sub":"b028bc4e-8df0-4b29-959d-000000000004","role":"authenticated","aal":"aal2"}',true);
set local role authenticated;
do $$ begin if qscreen_private.is_admin() then raise exception 'Email alone granted admin';end if;end $$;
reset role;
select qscreen_private.provision_admin('b028bc4e-8df0-4b29-959d-000000000004');
do $$ begin
    begin insert into qscreen_private.admin_identity(singleton,user_id) values(true,'b028bc4e-8df0-4b29-959d-000000000002');raise exception 'Second admin allowed';exception when unique_violation then null;end;
    if (select count(*) from qscreen_private.admin_audit_events where action='admin.provisioned')<>1 then raise exception 'Bootstrap audit missing';end if;
end $$;
set local role authenticated;
do $$ begin if not qscreen_private.is_admin() then raise exception 'Verified singleton admin with AAL2 denied';end if;end $$;
reset role;

select set_config('request.jwt.claims','{"sub":"b028bc4e-8df0-4b29-959d-000000000004","role":"authenticated","aal":"aal1"}',true);
set local role authenticated;
do $$ begin if qscreen_private.is_admin() then raise exception 'Admin without AAL2 allowed';end if;end $$;
reset role;
update auth.users set email='qscreen-changed-owner@example.invalid' where id='b028bc4e-8df0-4b29-959d-000000000004';
select set_config('request.jwt.claims','{"sub":"b028bc4e-8df0-4b29-959d-000000000004","role":"authenticated","aal":"aal2"}',true);
set local role authenticated;
do $$ begin if qscreen_private.is_admin() then raise exception 'Changed admin email retained privileges';end if;end $$;
reset role;

rollback;
select 'PASS: own-profile RLS, column grants, verification, disabled accounts, role injection rejection, bootstrap restrictions, singleton administrator, MFA requirement and audit; fixtures rolled back' as result;
