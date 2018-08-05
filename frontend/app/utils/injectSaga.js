import React from 'react';
import PropTypes from 'prop-types';

import getInjectors from './sagaInjectors';

/**
 * Dynamically injects a saga, passes component's props as saga arguments
 *
 * @param {string} key A key of the saga
 * @param {function} saga A root saga that will be injected
 * @param {string} [mode]
 *   - constants.DAEMON (default): starts on componentWillMount
 *   - constants.RESTART_ON_REMOUNT: starts on each componentWillMount, cancel on each componentWillUnmount.
 *   - constants.ONCE_TILL_UNMOUNT: starts on first componentWillMount, cancel on each componentWillUnmount.
 *
 */
export default ({ key, saga, mode }) => (WrappedComponent) => {
  class InjectSaga extends React.Component {
    static WrappedComponent = WrappedComponent;
    static contextTypes = {
      store: PropTypes.object.isRequired,
    };
    static displayName = `withSaga(${(WrappedComponent.displayName || WrappedComponent.name || 'Component')})`;

    componentWillMount() {
      const { injectSaga } = this.injectors;

      injectSaga(key, { saga, mode }, this.props);
    }

    componentWillUnmount() {
      const { ejectSaga } = this.injectors;

      ejectSaga(key);
    }

    injectors = getInjectors(this.context.store);

    render() {
      return <WrappedComponent {...this.props} />;
    }
  }

  return InjectSaga;
};
