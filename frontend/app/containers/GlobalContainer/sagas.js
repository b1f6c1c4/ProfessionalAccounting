import { call, put, takeEvery } from 'redux-saga/effects';
import * as api from 'utils/request';

import * as GLOBAL_CONTAINER from './constants';
import * as globalContainerActions from './actions';

// Sagas
export function* handleEtcdRequest() {
  try {
    const result = yield call(api.query); // TODO
    yield put(globalContainerActions.etcdSuccess(result));
  } catch (err) {
    yield put(globalContainerActions.etcdFailure(err));
  }
}

// Watcher
/* eslint-disable func-names */
export default function* watcher() {
  yield takeEvery(GLOBAL_CONTAINER.ETCD_REQUEST, handleEtcdRequest);
}
