import { createSelector } from 'reselect';

export const ListHash = () => createSelector(
  (state) => state.getIn(['globalContainer', 'etcd']),
  (etcd) => { }, // TODO
);

export const ListProj = () => createSelector(
  (state) => state.getIn(['globalContainer', 'etcd']),
  (etcd) => { }, // TODO
);
