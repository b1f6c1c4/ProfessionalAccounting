/* Copyright (C) 2020-2024 b1f6c1c4
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

ReadableStream.prototype[Symbol.asyncIterator] = async function* () {
  const reader = this.getReader()
  try {
    while (true) {
      const {done, value} = await reader.read()
      if (done) return
      yield value
    }
  }
  finally {
    reader.releaseLock()
  }
};

let theUser = window.localStorage.getItem('user') || 'anonymous';

const login = (u) => {
  theUser = u;
  document.getElementById('user').innerText = u;
  window.localStorage.setItem('user', u);
};

const send = (method, url, spec, body) => {
  const d = new Date();
  const ld = new Date(+d - 1000*60*d.getTimezoneOffset());
  const ldt = ld.toISOString().replace(/Z$/, '');
  const headers = {
    'Content-Type': 'text/plain',
    'X-User': theUser,
    'X-ClientDateTime': ldt,
  };
  if (spec)
    headers['X-Serializer'] = spec;
  return fetch(url, { method, body, headers });
};

const fancyxhr = (cb) => async (...args) => {
  const resp = await send(...args);
  if (!resp.ok) {
    cb(undefined, 'HTTP ' + resp.status + '\n' + await resp.text());
  } else {
    const decoder = new TextDecoderStream();
    resp.body.pipeTo(decoder.writable);
    for await (const chunk of decoder.readable)
      cb(chunk, undefined, false);
    cb(undefined, undefined, true);
  }
};

const xhr = async (...args) => {
  const resp = await send(...args);
  if (!resp.ok) {
    throw 'HTTP ' + resp.status + '\n' + await resp.text();
  } else {
    return resp.text();
  }
};

const execute = (cmd, cb) => {
  if (cmd === '') {
    return xhr('GET', '/api/emptyVoucher');
  }
  if (cmd === '??') {
    const t = `客户端帮助文档

??                          客户端帮助文档
login|user ...              选择用户
SPEC -- ...                 临时选择序列化器
`;
    if (cb) return cb(t, undefined, true);
    else return Promise.resolve(t);
  }

  const mx = cmd.match(/^(?:login|user) (.*)$/);
  if (mx) {
    login(mx[1]);
    const t = `Login as ${theUser}`;
    if (cb) return cb(t, undefined, true);
    else return Promise.resolve(t);
  }

  const m = cmd.match(/^([a-z](?:[^-]|-[^-]|---)+)--(.*)$/);
  const expr = m ? m[2] : cmd;
  const spec = m ? m[1] : null;

  return (cb ? fancyxhr(cb) : xhr)('POST', '/api/execute', spec, expr);
};

const sanitize = (raw) => {
  let str = raw;
  str = str.trim();
  str = str.substr(1, str.length - 2);
  if (str.startsWith('new Voucher')) {
    return { str, type: 'voucher' };
  }
  if (str.startsWith('new Asset')) {
    return { str, type: 'asset' };
  }
  if (str.startsWith('new Amortization')) {
    return { str, type: 'amort' };
  }
  return { str };
};

const upsert = (raw) => {
  const { str, type } = sanitize(raw);
  if (!type) {
    return Promise.reject('Type not found');
  }
  return xhr('POST', `/api/${type}Upsert`, null, str);
};

const remove = (raw) => {
  const { str, type } = sanitize(raw);
  if (!type) {
    return Promise.reject('Type not found');
  }
  return xhr('POST', `/api/${type}Removal`, null, str);
};
