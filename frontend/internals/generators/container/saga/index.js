/**
 * Container saga generator
 */

module.exports = {
  description: 'Add a saga to a container',
  prompts: [{
    type: 'input',
    name: 'name',
    default: 'FormContainer',
    message: 'Name of the container?',
    validate: (value) => {
      if ((/.+/).test(value)) {
        return true;
      }

      return 'The name is required';
    },
  }, {
    type: 'input',
    name: 'sagaName',
    default: 'ext',
    message: 'Name of the saga?',
  }, {
    type: 'input',
    name: 'sagaParam',
    default: 'id',
    message: 'Param of the saga?',
  }, {
    type: 'confirm',
    name: 'confirm',
    default: true,
    message: 'Are you sure?',
  }],
  actions: (data) => {
    if (!data.confirm) {
      return [];
    }

    const actions = [];

    // constants.js
    actions.push({
      type: 'complexModify',
      method: 'sectionEnd',
      indent: 0,
      section: /^\/\/ Sagas/g,
      pattern: /^\/\/ [A-Z][a-zA-Z]*$/g,
      path: '../../app/containers/{{ properCase name }}/constants.js',
      templateFile: './container/saga/constants.js.hbs',
      abortOnFail: true,
    });

    // actions.js
    actions.push({
      type: 'complexModify',
      method: 'sectionEnd',
      indent: 0,
      section: /^\/\/ Sagas/g,
      pattern: /^\/\/ [A-Z][a-zA-Z]*$/g,
      path: '../../app/containers/{{ properCase name }}/actions.js',
      templateFile: './container/saga/actions.js.hbs',
      abortOnFail: true,
    });

    // reducer.js
    actions.push({
      type: 'complexModify',
      method: 'sectionEnd',
      indent: 4,
      postPadding: false,
      section: /^ {4}\/\/ Sagas/g,
      pattern: /^ {4}\/\/ [A-Z][a-zA-Z]*$/g,
      path: '../../app/containers/{{ properCase name }}/reducer.js',
      templateFile: './container/saga/reducer.js.hbs',
      abortOnFail: true,
    });

    // sagas.js
    actions.push({
      type: 'complexModify',
      method: 'sectionEnd',
      indent: 0,
      section: /^\/\/ Sagas/g,
      pattern: /^\/\/ [A-Z][a-zA-Z]*$/g,
      path: '../../app/containers/{{ properCase name }}/sagas.js',
      templateFile: './container/saga/sagas.js.hbs',
      abortOnFail: true,
    });
    actions.push({
      type: 'complexModify',
      method: 'lastOccurance',
      pattern: /^ {2}yield take.*REQUEST.*Request\);$/g,
      path: '../../app/containers/{{ properCase name }}/sagas.js',
      template: '  yield takeEvery({{ constantCase name }}.{{ constantCase sagaName }}_REQUEST, handle{{ properCase sagaName }}Request);',
      abortOnFail: true,
    });

    // api.graphql
    actions.push({
      type: 'complexModify',
      method: 'append',
      path: '../../app/containers/{{properCase name}}/api.graphql',
      templateFile: './container/saga/api.graphql.hbs',
      abortOnFail: true,
    });

    return actions;
  },
};
