import {
  USER_REQUEST_GET_FAILURE,
  USER_REQUEST_GET_SUCCESS,
  UserRequestState,
} from "../types/userRquestTypes";
import commonReducer from "./commonReducer";

const initialState: UserRequestState = {};

const userRequestReducer = (
  state = initialState,
  action: any
): UserRequestState => {
  switch (action.type) {
    case USER_REQUEST_GET_SUCCESS:
      return {
        ...state,
        userRequest: action.userRequest,
      };
    case USER_REQUEST_GET_FAILURE:
      return {
        ...initialState,
        error: action.error,
      };
    default:
      return commonReducer(state, initialState, action);
  }
};

export default userRequestReducer;
