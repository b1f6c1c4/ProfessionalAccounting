/* Copyright (C) 2020-2025 b1f6c1c4
 *
 * This file is part of ProfessionalAccounting.
 *
 * ProfessionalAccounting is free software: you can redistribute it and/or
 * modify it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, version 3.
 *
 * ProfessionalAccounting is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Affero General Public License
 * for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with ProfessionalAccounting.  If not, see
 * <https://www.gnu.org/licenses/>.
 */

function a64url(base64url) {
  const padding = '='.repeat((4 - base64url.length % 4) % 4);
  const base64 = (base64url + padding).replace(/-/g, '+').replace(/_/g, '/');
  return Uint8Array.from(atob(base64), c => c.charCodeAt(0));
}

function b64url(o) {
  if (o === null)
    return null;
  return btoa(String.fromCharCode(...new Uint8Array(o)))
    .replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}

async function assignOptions(resp) {
  document.getElementById('domain').innerText = resp.headers.get('X-ServerName');
  const obj = await resp.json();
  obj.challenge = a64url(obj.challenge);
  if (typeof obj.user === 'object')
    obj.user.id = a64url(obj.user.id);
  if (Array.isArray(obj.allowCredentials)) {
    obj.allowCredentials.forEach((cred) => {
      cred.id = a64url(cred.id);
    });
  }
  if (Array.isArray(obj.excludeCredentials)) {
    obj.excludeCredentials.forEach((cred) => {
      cred.id = a64url(cred.id);
    });
  }
  return obj;
}

async function acceptInvitation() {
  const userHandle = new URLSearchParams(window.location.search).get('q');
  if (!userHandle) {
    throw new Error('Invalid URL');
  }
  const resp = await fetch(`/authn/at?q=${userHandle}`, { method: 'POST' });
  if (!resp.ok) {
    throw new Error(`HTTP/${resp.status} ${resp.statusText}:\n${await resp.text()}`);
  }
  const publicKey = await assignOptions(resp);
  return async () => {
    const credential = await navigator.credentials.create({ publicKey });
    const res = await fetch(`/authn/at?q=${userHandle}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        id: credential.id,
        rawId: b64url(credential.rawId),
        type: credential.type,
        extensions: credential.getClientExtensionResults(),
        response: {
          attestationObject: b64url(credential.response.attestationObject),
          clientDataJSON: b64url(credential.response.clientDataJSON),
          transports: credential.response.getTransports(),
        },
      }),
    });

    if (!res.ok) {
      throw new Error(`HTTP/${res.status} ${res.statusText}:\n${await res.text()}`);
    }
    window.location.href = '/login';
  };
}

async function login() {
  const resp = await fetch(`/authn/as`, { method: 'POST' });
  if (!resp.ok) {
    throw new Error(`HTTP/${resp.status} ${resp.statusText}:\n${await resp.text()}`);
  }
  const publicKey = await assignOptions(resp);
  const credential = await navigator.credentials.get({ publicKey });
  const res = await fetch('/authn/as', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      id: credential.id,
      rawId: b64url(credential.rawId),
      type: credential.type,
      extensions: credential.getClientExtensionResults(),
      response: {
        authenticatorData: b64url(credential.response.authenticatorData),
        clientDataJSON: b64url(credential.response.clientDataJSON),
        userHandle: b64url(credential.response.userHandle),
        signature: b64url(credential.response.signature),
      },
    }),
  });

  if (!res.ok) {
    throw new Error(`HTTP/${res.status} ${res.statusText}:\n${await res.text()}`);
  }
}
