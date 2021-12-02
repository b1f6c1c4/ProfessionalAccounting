/* Copyright (C) 2020-2021 b1f6c1c4
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

let theUser = window.localStorage.getItem('user') || 'anonymous';

const login = (u) => {
  theUser = u;
  document.getElementById('user').innerText = u;
  window.localStorage.setItem('user', u);
};

const xhr = (method, url, spec, payload) => new Promise((resolve, reject) => {
  const xhr = new XMLHttpRequest();
  xhr.onreadystatechange = function () {
    if (this.readyState === 4) {
      if (this.status === 200 || this.status === 204) {
        resolve(this.responseText);
      } else {
        reject('HTTP ' + this.status + '\n' + this.responseText);
      }
    }
  };
  xhr.open(method, url, true);
  const d = new Date();
  const ld = new Date(+d - 1000*60*d.getTimezoneOffset());
  const ldt = ld.toISOString().replace(/Z$/, '');
  xhr.setRequestHeader('X-User', theUser);
  xhr.setRequestHeader('X-ClientDateTime', ldt);
  if (spec)
    xhr.setRequestHeader('X-Serializer', spec);
  xhr.send(payload);
});

const execute = (cmd) => {
  if (cmd === '') {
    return xhr('GET', '/api/emptyVoucher');
  }
  if (cmd === '??') {
    return Promise.resolve(`客户端帮助文档

??                          客户端帮助文档
login|user ...              选择用户
SPEC -- ...                 临时选择序列化器
`);
  }

  const mx = cmd.match(/^(?:login|user) (.*)$/);
  if (mx) {
    login(mx[1]);
    return Promise.resolve(`Login as ${theUser}`);
  }

  const m = cmd.match(/^([a-z](?:[^-]|-[^-]|---)+)--(.*)$/);
  const expr = m ? m[2] : cmd;
  const spec = m ? m[1] : null;

  return xhr('POST', '/api/execute', spec, expr);
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
