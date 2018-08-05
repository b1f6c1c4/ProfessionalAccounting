/**
 * generator/index.js
 *
 * Exports the generators so plop knows them
 */

const _ = require('lodash');
const fs = require('fs');
const path = require('path');
const componentGenerator = require('./component/index.js');
const containerGenerator = require('./container/index.js');
const selectorGenerator = require('./container/selector/index.js');
const actionGenerator = require('./container/action/index.js');
const sagaGenerator = require('./container/saga/index.js');
const complexModify = require('./utils/complexModify.js');

const partialHelper = (plop) => (d) => {
  const name = _.camelCase(d
    .replace(`${__dirname}/`, '')
    .replace(/^(component|container)/, '')
    .replace(/(\.js)?\.hbs$/, ''));
  const content = fs.readFileSync(d, 'utf8');
  plop.setPartial(name, content);
};

const listPartials = [];
const makeListPartials = (d, lv) => {
  const files = fs.readdirSync(d);
  files.forEach((f) => {
    if (!/^([a-zA-Z0-9.]+\.hbs|[a-zA-Z]+)$/.test(f)) return;
    const fn = `${d}/${f}`;
    if (fs.statSync(fn).isDirectory()) {
      makeListPartials(fn, lv + 1);
    } else if (lv) {
      listPartials.push(fn);
    }
  });
};
makeListPartials(__dirname, 0);

module.exports = (plop) => {
  plop.setActionType('complexModify', complexModify);
  plop.setGenerator('component', componentGenerator);
  plop.setGenerator('container', containerGenerator);
  plop.setGenerator('selector', selectorGenerator);
  plop.setGenerator('action', actionGenerator);
  plop.setGenerator('saga', sagaGenerator);
  plop.setHelper('directory', (comp) => {
    try {
      fs.accessSync(path.join(__dirname, `../../app/containers/${comp}`), fs.F_OK);
      return `containers/${comp}`;
    } catch (e) {
      return `components/${comp}`;
    }
  });
  plop.setHelper('curly', (open) => (open ? '{' : '}'));
  plop.setHelper('ifOr', function ifOr(v1, v2, options) {
    if (v1 || v2) {
      return options.fn(this);
    }
    return options.inverse(this);
  });
  listPartials.map(partialHelper(plop));
};
