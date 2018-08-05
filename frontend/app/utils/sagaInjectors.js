import _ from 'lodash';
import invariant from 'invariant';

import checkStore from './checkStore';
import {
  DAEMON,
  ONCE_TILL_UNMOUNT,
  RESTART_ON_REMOUNT,
} from './constants';

const allowedModes = [RESTART_ON_REMOUNT, DAEMON, ONCE_TILL_UNMOUNT];

const checkKey = (key) => invariant(
  _.isString(key) && !_.isEmpty(key),
  '(app/utils...) injectSaga: Expected `key` to be a non empty string',
);

const checkDescriptor = (descriptor) => {
  const shape = {
    saga: _.isFunction,
    mode: (mode) => _.isString(mode) && allowedModes.includes(mode),
  };
  invariant(
    _.conformsTo(descriptor, shape),
    '(app/utils...) injectSaga: Expected a valid saga descriptor',
  );
};

export const injectSagaFactory = (store, isValid) => (key, { saga, mode = DAEMON, ...other }, args) => {
  if (!isValid) checkStore(store);

  const descriptor = { saga, mode, ...other };
  checkKey(key);
  checkDescriptor(descriptor);

  let hasSaga = Reflect.has(store.injectedSagas, key);

  if (process.env.NODE_ENV !== 'production') {
    const oldDescriptor = store.injectedSagas[key];
    // enable hot reloading of daemon and once-till-unmount sagas
    if (hasSaga && oldDescriptor.saga !== saga) {
      oldDescriptor.task.cancel();
      hasSaga = false;
    }
  }

  if (!hasSaga || mode === RESTART_ON_REMOUNT) {
    _.set(store.injectedSagas, key, { ...descriptor, task: store.runSaga(saga, args) });
  }
};

export const ejectSagaFactory = (store, isValid) => (key) => {
  if (!isValid) checkStore(store);

  checkKey(key);

  if (!Reflect.has(store.injectedSagas, key)) {
    return;
  }

  const descriptor = store.injectedSagas[key];

  if (descriptor.mode === DAEMON) {
    return;
  }

  descriptor.task.cancel();
};

export default function getInjectors(store) {
  checkStore(store);

  return {
    injectSaga: injectSagaFactory(store, true),
    ejectSaga: ejectSagaFactory(store, true),
  };
}
