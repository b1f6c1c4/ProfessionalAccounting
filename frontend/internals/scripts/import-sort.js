/* eslint-disable no-console */
/* eslint-disable no-restricted-syntax */
const { exec } = require('shelljs');
const fs = require('fs');
const async = require('async');
const sortImports = require('@b1f6c1c4/import-sort').default;
const parser = require('import-sort-parser-babylon');

const listFiles = (t) => new Promise((resolve, reject) => {
  exec(`git ls-tree -r --name-only HEAD ${t}`, {
    silent: true,
  }, (code, stdout, stderr) => {
    if (code) {
      console.log(stdout);
      reject(new Error(stderr));
    } else {
      resolve(stdout.split('\n'));
    }
  });
});

const loadFile = (ftmp) => new Promise((resolve, reject) => {
  fs.readFile(ftmp, 'utf-8', (err, txt) => {
    if (err) {
      reject(err);
      return;
    }
    resolve(txt);
  });
});

const writeFile = (ftmp, txt) => new Promise((resolve, reject) => {
  fs.writeFile(ftmp, txt, 'utf-8', (err) => {
    if (err) {
      reject(err);
      return;
    }
    resolve();
  });
});

const style = (file) => (styleApi) => {
  const {
    and,
    not,
    name,
    moduleName,
    isAbsoluteModule,
    hasOnlyDefaultMember,
    hasOnlyNamespaceMember,
    unicode,
  } = styleApi;

  const moduleNameIs = (nm) => {
    if (typeof nm === 'string') {
      return moduleName((m) => m === nm);
    }
    return moduleName((m) => nm.test(m));
  };

  const proper = (a, b) => {
    const la = /^[a-z]/.test(a);
    const lb = /^[a-z]/.test(b);
    if (la && !lb) return -1;
    if (!la && lb) return +1;
    if (a === b) return 0;
    if (b.startsWith(a)) return -1;
    if (a.startsWith(b)) return +1;
    return unicode(a, b);
  };

  return [
    { match: moduleNameIs('babel-polyfill') },
    {
      match: and(
        moduleNameIs(/\.s?css$/),
        isAbsoluteModule,
      ),
      sort: moduleName(unicode),
    },
    { separator: true },

    { match: moduleNameIs('lodash') },
    { match: moduleNameIs('react') },
    { match: moduleNameIs('prop-types') },
    { match: moduleNameIs('react-dom') },
    { match: moduleNameIs('immutable') },
    { match: moduleNameIs('redux') },
    {
      match: moduleNameIs('redux-saga'),
      sortNamedMembers: name(proper),
    },
    {
      match: moduleNameIs('redux-saga/effects'),
      sortNamedMembers: name(proper),
    },
    { match: moduleNameIs('utils/request') },
    { match: moduleNameIs('react-redux') },
    { match: moduleNameIs('react-router') },
    { match: moduleNameIs('react-router-redux') },
    { match: moduleNameIs('reselect') },
    { match: moduleNameIs('react-intl') },
    {
      match: and(
        () => /containers/.test(file),
        moduleNameIs(/^[-a-z0-9]+$/),
      ),
      sort: moduleName(unicode),
    },
    { match: moduleNameIs('utils/injectSaga') },
    { match: moduleNameIs('utils/permission') },
    {
      match: and(
        () => /containers/.test(file),
        moduleNameIs(/^utils\//),
      ),
    },
    { separator: true },

    {
      match: moduleNameIs('material-ui'),
      sortNamedMembers: name(proper),
    },
    { match: moduleNameIs(/^material-ui\/colors/) },
    {
      match: moduleNameIs('@material-ui/icons'),
      sortNamedMembers: name(proper),
    },
    { match: moduleNameIs('classnames') },
    { match: moduleNameIs('react-router-dom') },
    { match: moduleNameIs('react-file-reader') },
    { match: moduleNameIs('react-jsonschema-form') },
    {
      match: and(
        () => /components/.test(file),
        moduleNameIs(/^[-a-z0-9.]+$/),
      ),
      sort: moduleName(unicode),
    },
    {
      match: and(
        moduleNameIs(/^components\//),
        hasOnlyDefaultMember,
      ),
      sort: moduleName(unicode),
    },
    {
      match: and(
        moduleNameIs(/^containers\//),
        not(moduleNameIs(/^containers\/.*\/Loadable$/)),
        hasOnlyDefaultMember,
      ),
      sort: moduleName(unicode),
    },
    {
      match: and(
        moduleNameIs(/^containers\/.*\/Loadable$/),
        hasOnlyDefaultMember,
      ),
      sort: moduleName(unicode),
    },
    { separator: true },

    {
      match: and(
        moduleNameIs(/^containers\/[A-Za-z]+\/constants/),
        hasOnlyNamespaceMember,
      ),
      sort: moduleName(unicode),
    },
    {
      match: and(
        moduleNameIs(/^containers\/[A-Za-z]+\/selectors/),
        hasOnlyNamespaceMember,
      ),
      sort: moduleName(unicode),
    },
    {
      match: and(
        moduleNameIs(/^containers\/[A-Za-z]+\/actions/),
        hasOnlyNamespaceMember,
      ),
      sort: moduleName(unicode),
    },
    { match: moduleNameIs(/^\.\.?\/constants/) },
    { match: moduleNameIs(/^\.\.?\/selectors/) },
    { match: moduleNameIs(/^\.\.?\/actions/) },
    { match: moduleNameIs(/^\.\.?\/reducer/) },
    {
      match: and(
        moduleNameIs(/^\.\.?\/sagas/),
        hasOnlyDefaultMember,
      ),
    },
    { match: moduleNameIs(/^\.\.?\/api.graphql/) },
    { separator: true },

    {
      match: isAbsoluteModule,
      sort: moduleName(unicode),
    },
    { separator: true },

    {
      match: and(
        moduleNameIs(/^\.\.?\/sagas/),
        not(hasOnlyDefaultMember),
      ),
    },
    { separator: true },
  ];
};

const sortFile = async (s) => {
  const txt = await loadFile(s);
  const { code, changes } = sortImports(
    txt,
    parser,
    style(s),
    process.cwd(),
    {},
  );
  if (changes.length) {
    await writeFile(s, code);
  }
};

const run = async () => {
  const [f1, f2] = await Promise.all([
    listFiles('app/containers'),
    listFiles('app/components'),
  ]);
  const f = [
    'app/app.js',
    'app/root.js',
  ].concat(f1).concat(f2).filter((s) => /(?<!messages)\.js$/.test(s));
  await async.mapLimit(f, 10, sortFile);
};

if (process.argv.length === 2) {
  run().catch((err) => {
    console.error(err);
  });
} else {
  async.mapLimit(process.argv.splice(2), 10, sortFile, (err) => {
    if (err) {
      console.error(err);
    }
  });
}
