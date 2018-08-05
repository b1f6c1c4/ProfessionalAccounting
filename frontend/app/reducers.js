import { combineReducers } from 'redux-immutable';
import { fromJS } from 'immutable';
import { LOCATION_CHANGE } from 'react-router-redux';

import globalContainerReducer from 'containers/GlobalContainer/reducer';
import viewProjContainerReducer from 'containers/ViewProjContainer/reducer';
import viewCatContainerReducer from 'containers/ViewCatContainer/reducer';
import viewEvalContainerReducer from 'containers/ViewEvalContainer/reducer';
import homeContainerReducer from 'containers/HomeContainer/reducer';
import runContainerReducer from 'containers/RunContainer/reducer';
import uploadContainerReducer from 'containers/UploadContainer/reducer';

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
    viewProjContainer: viewProjContainerReducer,
    viewCatContainer: viewCatContainerReducer,
    viewEvalContainer: viewEvalContainerReducer,
    homeContainer: homeContainerReducer,
    runContainer: runContainerReducer,
    uploadContainer: uploadContainerReducer,
  });

  return appReducer;
}
