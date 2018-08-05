/**
 * Container action generator
 */

module.exports = {
  description: 'Add an action to a container',
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
    name: 'actionName',
    default: 'toggle open',
    message: 'Name of the action?',
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
      section: /^\/\/ Actions/g,
      pattern: /^\/\/ [A-Z][a-zA-Z]*$/g,
      path: '../../app/containers/{{ properCase name }}/constants.js',
      templateFile: './container/action/constants.js.hbs',
      prePadding: false,
      postPadding: false,
      abortOnFail: true,
    });

    // actions.js
    actions.push({
      type: 'complexModify',
      method: 'sectionEnd',
      indent: 0,
      section: /^\/\/ Actions/g,
      pattern: /^\/\/ [A-Z][a-zA-Z]*$/g,
      path: '../../app/containers/{{ properCase name }}/actions.js',
      templateFile: './container/action/actions.js.hbs',
      abortOnFail: true,
    });

    // reducer.js
    actions.push({
      type: 'complexModify',
      method: 'sectionEnd',
      indent: 4,
      postPadding: false,
      section: /^ {4}\/\/ Actions/g,
      pattern: /^ {4}\/\/ [A-Z][a-zA-Z]*$/g,
      path: '../../app/containers/{{ properCase name }}/reducer.js',
      templateFile: './container/action/reducer.js.hbs',
      abortOnFail: true,
    });

    // index.js
    actions.push({
      type: 'complexModify',
      method: 'lastOccurance',
      pattern: /^ {2}on[a-zA-Z]+: PropTypes/g,
      path: '../../app/containers/{{ properCase name }}/index.js',
      template: '  on{{ properCase actionName }}Action: PropTypes.func.isRequired,',
      abortOnFail: true,
    });
    actions.push({
      type: 'complexModify',
      method: 'lastOccurance',
      pattern: /^ {4}on[a-zA-Z]+: \(\) => dispatch/g,
      path: '../../app/containers/{{ properCase name }}/index.js',
      template: '    on{{ properCase actionName }}Action: () => dispatch({{ camelCase name }}Actions.{{ camelCase actionName }}()),',
      abortOnFail: true,
    });

    return actions;
  },
};
