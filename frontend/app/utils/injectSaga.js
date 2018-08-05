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

    injectors = getInjectors(this.context.store);

    static displayName = `withSaga(${(WrappedComponent.displayName || WrappedComponent.name || 'Component')})`;

    static contextTypes = {
      store: PropTypes.object.isRequired,
    };

    componentWillMount() {
      const { injectSaga } = this.injectors;

      injectSaga(key, { saga, mode }, this.props);
    }

    componentWillUnmount() {
      const { ejectSaga } = this.injectors;

      ejectSaga(key);
    }

    render() {
      return <WrappedComponent {...this.props} />;
    }
  }

  return InjectSaga;
};
