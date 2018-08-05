/**
 * Container Generator
 */

const componentExists = require('../utils/componentExists');

module.exports = {
  description: 'Add a container (smart) component',
  prompts: [{
    type: 'input',
    name: 'name',
    default: 'FormContainer',
    message: 'What should it be called?',
    validate: (value) => {
      if ((/.+/).test(value)) {
        return componentExists(value) ? 'A component or container with this name already exists' : true;
      }

      return 'The name is required';
    },
  }, {
    type: 'confirm',
    name: 'wantMSelectors',
    default: false,
    message: 'Do you want memorized selectors?',
  }, {
    type: 'input',
    name: 'mselectorName',
    when: (ans) => ans.wantMSelectors,
    default: 'complex data',
    message: 'Memorized selector name?',
  }, {
    type: 'confirm',
    name: 'wantReducer',
    default: true,
    message: 'Do you want reducer? (Must enable for sagas or actions)',
  }, {
    type: 'confirm',
    name: 'wantActions',
    default: false,
    message: 'Do you want actions?',
  }, {
    type: 'input',
    name: 'actionName',
    when: (ans) => ans.wantActions,
    default: 'toggle',
    message: 'Action name?',
  }, {
    type: 'confirm',
    name: 'wantSagas',
    when: (ans) => ans.wantReducer,
    default: true,
    message: 'Do you want sagas?',
  }, {
    type: 'input',
    name: 'sagaName',
    when: (ans) => ans.wantSagas,
    default: 'external',
    message: 'Saga name?',
  }, {
    type: 'input',
    name: 'sagaParam',
    when: (ans) => ans.wantSagas,
    default: 'id',
    message: 'Param of the saga?',
  }, {
    type: 'confirm',
    name: 'wantLoadable',
    default: true,
    message: 'Do you want to load resources asynchronously?',
  }],
  actions: (data) => {
    if (data.wantReducer && !(data.wantActions || data.wantSagas)) {
      throw new Error('Enable either action or saga, or both.');
    }

    const actions = [];

    // Generate index.js
    actions.push({
      type: 'add',
      path: '../../app/containers/{{properCase name}}/index.js',
      templateFile: './container/index.js.hbs',
      abortOnFail: true,
    });

    if (data.wantMSelectors) {
      // Generate selectors.js
      actions.push({
        type: 'add',
        path: '../../app/containers/{{properCase name}}/selectors.js',
        templateFile: './container/selectors.js.hbs',
        abortOnFail: true,
      });
    }

    if (data.wantReducer) {
      // Generate constants.js
      actions.push({
        type: 'add',
        path: '../../app/containers/{{properCase name}}/constants.js',
        templateFile: './container/constants.js.hbs',
        abortOnFail: true,
      });

      // Generate actions.js
      actions.push({
        type: 'add',
        path: '../../app/containers/{{properCase name}}/actions.js',
        templateFile: './container/actions.js.hbs',
        abortOnFail: true,
      });

      // Generate reducer.js
      actions.push({
        type: 'add',
        path: '../../app/containers/{{properCase name}}/reducer.js',
        templateFile: './container/reducer.js.hbs',
        abortOnFail: true,
      });

      if (data.wantSagas) {
        // Generate sagas.js
        actions.push({
          type: 'add',
          path: '../../app/containers/{{properCase name}}/sagas.js',
          templateFile: './container/sagas.js.hbs',
          abortOnFail: true,
        });

        // Generate api.graphql
        actions.push({
          type: 'add',
          path: '../../app/containers/{{properCase name}}/api.graphql',
          templateFile: './container/api.graphql.hbs',
          abortOnFail: true,
        });
      }
    }

    if (data.wantLoadable) {
      // Generate loadable.js
      actions.push({
        type: 'add',
        path: '../../app/containers/{{properCase name}}/Loadable.js',
        templateFile: './component/loadable.js.hbs',
        abortOnFail: true,
      });
    }

    actions.push({
      type: 'complexModify',
      method: 'lastOccurance',
      pattern: /^import [a-zA-Z]+Reducer from 'containers\/[A-Za-z]+\/reducer';$/g,
      path: '../../app/reducers.js',
      template: 'import {{ camelCase name }}Reducer from \'containers/{{ properCase name }}/reducer\';',
      abortOnFail: true,
    });
    actions.push({
      type: 'complexModify',
      method: 'lastOccurance',
      pattern: / {4}[a-zA-Z]+: [a-zA-Z]+Reducer,$/,
      path: '../../app/reducers.js',
      template: '    {{ camelCase name }}: {{ camelCase name }}Reducer,',
      abortOnFail: true,
    });

    return actions;
  },
};
