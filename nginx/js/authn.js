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

function getSSR() {
  const j = document.getElementById('ssr').textContent;
  const obj = JSON.parse(j);
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

async function register(userHandle) {
  const credential = await navigator.credentials.create({ publicKey: getSSR() });
  const res = await fetch(`/invite/${userHandle}`, {
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

  if (res.ok) {
    window.location.href = '/login';
  } else {
    return res.text();
  }
}

async function login() {
  const credential = await navigator.credentials.get({ publicKey: getSSR() });
  const res = await fetch('/login', {
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

  if (res.ok) {
    window.location.href = '/';
  } else {
    return res.text();
  }
}

function wrap(f) {
  return async () => {
    let txt;
    try {
      txt = await f(window.location.pathname.split('/').pop());
    } catch (err) {
      console.error(err);
      txt = err.message;
    }
    document.getElementById('err').style.visibility = 'visible';
    document.getElementById('res').innerText = txt;
  };
}
