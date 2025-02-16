import {
  USER_GET_FAILURE,
  USER_GET_SUCCESS,
  UserAction,
  UserState,
} from "../types/userTypes";
import commonReducer from "./commonReducer";

const initialState: UserState = {};

const userReducer = (state = initialState, action: UserAction): UserState => {
  switch (action.type) {
    case USER_GET_SUCCESS:
      return {
        ...state,
        user: action.user,
      };
    case USER_GET_FAILURE:
      return {
        ...initialState,
        error: action.error,
      };
    default:
      return commonReducer(state, initialState, action);
  }
};

export default userReducer;
