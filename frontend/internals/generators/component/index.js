/**
 * Component Generator
 */

const componentExists = require('../utils/componentExists');

module.exports = {
  description: 'Add an unconnected (dumb) component',
  prompts: [{
    type: 'input',
    name: 'name',
    default: 'FormPage',
    message: 'What should it be called?',
    validate: (value) => {
      if ((/.+/).test(value)) {
        return componentExists(value) ? 'A component or container with this name already exists' : true;
      }

      return 'The name is required';
    },
  }, {
    type: 'confirm',
    name: 'wantLoadable',
    default: false,
    message: 'Do you want to load resources asynchronously?',
  }],
  actions: (data) => {
    const actions = [];

    // Generate index.js
    actions.push({
      type: 'add',
      path: '../../app/components/{{properCase name}}/index.js',
      templateFile: './component/index.js.hbs',
      abortOnFail: true,
    });

    if (data.wantLoadable) {
      // Generate loadable.js
      actions.push({
        type: 'add',
        path: '../../app/components/{{properCase name}}/Loadable.js',
        templateFile: './component/loadable.js.hbs',
        abortOnFail: true,
      });
    }

    return actions;
  },
};
