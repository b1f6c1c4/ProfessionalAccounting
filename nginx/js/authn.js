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

const wait = (t) => new Promise((resolve) => setTimeout(resolve, t));
let publicKey;

function assignOptions(obj) {
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
  publicKey = obj;
}

async function register() {
  const userHandle = new URLSearchParams(window.location.search).get('q');
  if (!userHandle) {
    throw new Error('Invalid URL');
  }
  const resp = await fetch(`/authn/at/${userHandle}`, { method: 'POST' });
  if (!resp.ok) {
    throw new Error(`HTTP/${resp.status} ${resp.statusText}:\n${await resp.text()}`);
  }
  assignOptions(await resp.json());
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
      throw new Error('Failed to register:\n' + resp.text());
    }
    window.location.href = '/login';
  };
}

async function login() {
  const resp = await fetch(`/authn/as`, { method: 'POST' });
  if (!resp.ok) {
    throw new Error(`HTTP/${resp.status} ${resp.statusText}:\n${await resp.text()}`);
  }
  assignOptions(await resp.json());
  const credential = await navigator.credentials.get({ publicKey });
  const res = await fetch('/authn/as/', {
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

function wrap(f, errText) {
  window.onload = async () => {
    const main = document.querySelector('main');
    const showErr = (err) => {
      main.classList.add('err');
      const h = document.createElement('p');
      h.innerText = errText;
      main.innerText = err;
      if (main.childNodes.length) {
        main.insertBefore(h, main.childNodes[0]);
      } else {
        main.appendChild(h);
      }
    };
    try {
      const [func, _] = await Promise.all([ f(), wait(3000) ]);
      document.querySelector('.action').classList.add('show');
      document.querySelector('.action > .btn')
        .addEventListener('click', func().catch(showErr));
    } catch (err) {
      showErr(err);
    }
  };
}
