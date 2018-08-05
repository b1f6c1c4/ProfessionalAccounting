import { combineReducers } from 'redux-immutable';
import { fromJS } from 'immutable';
import { LOCATION_CHANGE } from 'react-router-redux';

import globalContainerReducer from 'containers/GlobalContainer/reducer';

const routeInitialState = fromJS({
  location: null,
});

function routeReducer(state = routeInitialState, action) {
  switch (action.type) {
    /* istanbul ignore next */
    case LOCATION_CHANGE:
      return state.set('location', fromJS(action.payload));
    default:
      return state;
  }
}

export default function createReducer() {
  const appReducer = combineReducers({
    route: routeReducer,
    globalContainer: globalContainerReducer,
  });

  return appReducer;
}
