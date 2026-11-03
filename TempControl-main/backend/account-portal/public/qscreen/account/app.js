"use strict";
// Tokens stay in this closure, never localStorage, cookies, logs or redirects.
(async () => {
  const fragment = new URLSearchParams(location.hash.slice(1));
  history.replaceState(null, "", location.pathname);
  let accessToken = fragment.get("access_token");
  const type = fragment.get("type");
  const hasError = fragment.has("error");
  for (const key of [...fragment.keys()]) fragment.delete(key);
  const title = document.getElementById("title");
  const status = document.getElementById("status");
  const form = document.getElementById("password-form");
  const password = document.getElementById("password");
  const confirmation = document.getElementById("confirmation");
  const save = document.getElementById("save");
  const config = window.QSCREEN_AUTH;
  const origin = "https://ybmesonqfclarqigwxfe.supabase.co";
  const fail = () => { accessToken = null; title.textContent = "Deze link werkt niet meer."; status.textContent = "Open de meest recente bevestigings- of herstelmail. Vraag zo nodig een nieuwe mail aan in Qscreen."; form.hidden = true; };
  async function request(method, path, data) {
    const controller = new AbortController();
    const timeout = setTimeout(() => controller.abort(), 12000);
    try {
      const response = await fetch(origin + path, { method, credentials: "omit", referrerPolicy: "no-referrer", cache: "no-store", redirect: "error", signal: controller.signal,
        headers: { apikey: config.publishableKey, Authorization: "Bearer " + accessToken, "Content-Type": "application/json" }, ...(data ? { body: JSON.stringify(data) } : {}) });
      if (!response.ok || Number(response.headers.get("Content-Length") || 0) > 131072) throw new Error("request_rejected");
      if (response.status === 204) return {};
      const reader = response.body.getReader(); let size = 0; const chunks = [];
      for (;;) { const { value, done } = await reader.read(); if (done) break; size += value.length; if (size > 131072) { await reader.cancel(); throw new Error("response_limit"); } chunks.push(value); }
      const bytes = new Uint8Array(size); let offset = 0; for (const chunk of chunks) { bytes.set(chunk, offset); offset += chunk.length; }
      return size ? JSON.parse(new TextDecoder().decode(bytes)) : {};
    } finally { clearTimeout(timeout); }
  }
  if (!config || config.origin !== origin || !/^sb_publishable_[A-Za-z0-9_-]+$/.test(config.publishableKey) || !accessToken || accessToken.length > 32768 || hasError) { fail(); return; }
  try {
    const user = await request("GET", "/auth/v1/user");
    if (!user.id || !user.email_confirmed_at) { fail(); return; }
    if (type === "recovery") { title.textContent = "Kies je nieuwe wachtwoord."; status.textContent = "Gebruik minstens 12 tekens. Je wachtwoord wordt rechtstreeks bij de accountprovider gewijzigd."; form.hidden = false; }
    else { title.textContent = "Je e-mailadres is bevestigd."; status.textContent = "Je kunt terug naar Qscreen Account en je aanmelden."; accessToken = null; }
  } catch { fail(); return; }
  form.addEventListener("submit", async (event) => {
    event.preventDefault();
    if (password.value.length < 12 || password.value !== confirmation.value) { status.textContent = "Gebruik minstens 12 tekens en vul tweemaal hetzelfde wachtwoord in."; return; }
    save.disabled = true;
    try { await request("PUT", "/auth/v1/user", { password: password.value }); password.value = confirmation.value = ""; form.hidden = true; title.textContent = "Je wachtwoord is gewijzigd."; status.textContent = "Ga terug naar Qscreen Account en meld je opnieuw aan.";
      try { await request("POST", "/auth/v1/logout?scope=local", {}); } catch { /* Never retain a recovery token to retry logout. */ } accessToken = null;
    } catch { status.textContent = "Wijzigen is niet gelukt. De link kan verlopen zijn; vraag zo nodig een nieuwe herstelmail aan."; }
    finally { password.value = confirmation.value = ""; save.disabled = false; }
  });
})();
