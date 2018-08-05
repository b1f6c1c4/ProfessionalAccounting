import * as GLOBAL_CONTAINER from './constants';

// Actions
export function openDrawer() {
  return {
    type: GLOBAL_CONTAINER.OPEN_DRAWER_ACTION,
  };
}

export function closeDrawer() {
  return {
    type: GLOBAL_CONTAINER.CLOSE_DRAWER_ACTION,
  };
}

// Sagas
export function etcdRequest() {
  return {
    type: GLOBAL_CONTAINER.ETCD_REQUEST,
  };
}

export function etcdSuccess(result) {
  return {
    type: GLOBAL_CONTAINER.ETCD_SUCCESS,
    result,
  };
}

export function etcdFailure(error) {
  return {
    type: GLOBAL_CONTAINER.ETCD_FAILURE,
    error,
  };
}
