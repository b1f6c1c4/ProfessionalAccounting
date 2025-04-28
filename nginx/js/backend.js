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
let theAssume = window.localStorage.getItem('assume') || undefined;

const updateUA = () => {
  document.getElementById('assume').innerText = theAssume || '';
  document.getElementById('user').innerText = theUser;
};
updateUA();

const login = async (u) => {
  theUser = u;
  updateUA();
  window.localStorage.setItem('user', theUser);
  let resp = await send('GET', '/api/execute', undefined, { q: 'me' });
  if (resp.status === 401) {
    const old = `----Server Response:----\nHTTP 401 Unauthorized\n${await resp.text()}`;
    try {
      await invokeAuthentication();
    } catch (e) {
      throw `${old}\n----During authentication:----\n${e}`;
    }
    resp = await send('GET', '/api/execute', undefined, { q: 'me' });
  }
  if (!resp.ok) {
    throw `HTTP ${resp.status}\n${await resp.text()}`;
  }
  return resp.text();
};

const assume = (u) => {
  if (u) {
    theAssume = u;
    window.localStorage.setItem('assume', u);
  } else {
    theAssume = undefined;
    window.localStorage.removeItem('assume');
  }
  updateUA();
};

const send = (method, url, body, params) => {
  const d = new Date();
  const ld = new Date(+d - 1000*60*d.getTimezoneOffset());
  const ldt = ld.toISOString().replace(/Z$/, '');
  const up = new URLSearchParams(params);
  !up.has('u') && up.set('u', theUser);
  !up.has('id') && theAssume && up.set('id', theAssume);
  const headers = {
    'Content-Type': 'text/plain',
    'X-ClientDateTime': ldt,
  };
  return fetch(`${url}?${up}`, { method, body, headers });
};

const fancyxhr = (cb) => async (...args) => {
  let resp = await send(...args);
  if (resp.status === 401) {
    const old = `----Server Response:----\nHTTP 401 Unauthorized\n${await resp.text()}`;
    try {
      await invokeAuthentication();
    } catch (e) {
      return cb(undefined, `${old}\n----During authentication:----\n${e}`);
    }
    resp = await send(...args);
  }
  if (!resp.ok) {
    cb(undefined, `HTTP ${resp.status}\n${await resp.text()}`);
  } else {
    const decoder = new TextDecoderStream();
    resp.body.pipeTo(decoder.writable);
    for await (const chunk of decoder.readable)
      cb(chunk, undefined, false);
    cb(undefined, undefined, true);
  }
};

const xhr = async (...args) => {
  let resp = await send(...args);
  if (resp.status === 401) {
    const old = `----Server Response:----\nHTTP 401 Unauthorized\n${await resp.text()}`;
    try {
      await invokeAuthentication();
    } catch (e) {
      throw `${old}\n----During authentication:----\n${e}`;
    }
    resp = await send(...args);
  }
  if (!resp.ok) {
    throw `HTTP ${resp.status}\n${await resp.text()}`;
  } else {
    return resp.text();
  }
};

const execute = (cmd, cb) => {
  if (cmd === '') {
    return xhr('GET', '/api/voucher');
  }
  if (cmd === '??') {
    const t = `客户端帮助文档

特殊命令：
?                           显示控制台帮助文档
??                          显示此客户端帮助文档
login                       认证当前身份，不改变默认记账主体
login   <记账主体>          认证当前身份并选择记账主体
assume  <身份>              代入其它身份（限系统管理员使用）
sudo ...                    临时提权至admin（限系统管理员使用）

以其他方式显示记账凭证：
csharp -- ...               使用C#序列化器显示记账凭证
json   -- ...               使用Json序列化器显示记账凭证
csv '<分隔符>' [<字段>...] -- ...     使用CSV序列化器显示记账凭证，其中<字段>含义如下：
  id         编号
  d|date     日期
  type       类型
  U|user     用户
  C|currency 币种
  t|title    一级科目代码
  t'         一级科目备注
  s|subtitle 二级科目代码
  s'         二级科目备注
  c|content  内容
  r|remark   备注
  v|fund     金额
  若不指定，默认为 csv '	' id d U C t t' s s' c r v

命令窗口快捷键：
Enter                       执行
Shift+Enter                 执行并将结果添加在后方
Ctrl+Alt+Enter              执行需要上传内容的命令，如$stmt$等

编辑窗口快捷键：
Alt+Enter                   保存
Alt+Delete                  删除
Ctrl+Alt+Shift+Enter        全部保存
Ctrl+Alt+Shift+Delete       全部删除
Ctrl+Alt+Enter              执行需要上传内容的命令，如$stmt$等
`;
    if (cb) return cb(t, undefined, true);
    else return Promise.resolve(t);
  }

  const matchL = cmd.match(/^login(?:\s+(.*))?$/);
  if (matchL) {
    const p = login(matchL[1] ?? theUser);
    if (cb) return p.then(
      (res) => cb('' + res, undefined, true),
      (err) => cb(undefined, `${err}`, true));
    else return p;
  }

  const matchA = cmd.match(/^assume(?: (.*))?$/);
  if (matchA) {
    assume(matchA[1]);
    const t = `Assume as ${theAssume}`;
    if (cb) return cb(t, undefined, true);
    else return Promise.resolve(t);
  }

  let expr = cmd;
  const obj = {};
  const m = cmd.match(/^(sudo\s+)?(?:([a-z](?:[^-]|-[^-]|---)+)--)?(.*)$/);
  if (m) {
    if (m[1]) obj.id = 'admin';
    if (m[2]) obj.spec = m[2];
    expr = m[3];
  }

  return (cb ? fancyxhr(cb) : xhr)('POST', '/api/execute', expr, obj);
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
  return xhr('POST', `/api/${type}`, str);
};

const remove = (raw) => {
  const { str, type } = sanitize(raw);
  if (!type) {
    return Promise.reject('Type not found');
  }
  return xhr('DELETE', `/api/${type}`, str);
};
