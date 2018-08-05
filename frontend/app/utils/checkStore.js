import _ from 'lodash';
import invariant from 'invariant';

/**
 * Validate the shape of redux store
 */
export default function checkStore(store) {
  const shape = {
    dispatch: _.isFunction,
    subscribe: _.isFunction,
    getState: _.isFunction,
    replaceReducer: _.isFunction,
    runSaga: _.isFunction,
    injectedSagas: _.isObject,
  };
  invariant(
    _.conformsTo(store, shape),
    '(app/utils...) injectors: Expected a valid redux store',
  );
}
