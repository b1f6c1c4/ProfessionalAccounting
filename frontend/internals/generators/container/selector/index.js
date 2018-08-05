/**
 * Container selector generator
 */

module.exports = {
  description: 'Add a selector to a container',
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
    name: 'mselectorName',
    default: 'filtered data',
    message: 'Name of the selector?',
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

    // selectors.js
    actions.push({
      type: 'complexModify',
      method: 'sectionEnd',
      indent: 0,
      section: /.*/g,
      pattern: /(?!)/g,
      path: '../../app/containers/{{ properCase name }}/selectors.js',
      templateFile: './container/selector/selectors.js.hbs',
      abortOnFail: true,
    });

    // index.js
    actions.push({
      type: 'complexModify',
      method: 'lastOccurance',
      pattern: /^ {2}[a-zA-Z]+: [a-zA-Z]+Selectors.[a-zA-Z]+\(\),$/g,
      path: '../../app/containers/{{ properCase name }}/index.js',
      template: '  {{ camelCase mselectorName }}: {{ camelCase name }}Selectors.{{ properCase mselectorName }}(),',
      abortOnFail: true,
    });

    return actions;
  },
};
