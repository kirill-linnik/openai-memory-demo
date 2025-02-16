import { Dispatch } from "redux";
import { UserRequest } from "../../models/UserRequest";
import { backendProvider, extractError } from "../../services/BackendProvider";
import {
  USER_REQUEST_GET_FAILURE,
  USER_REQUEST_GET_REQUEST,
  USER_REQUEST_GET_SUCCESS,
  UserRequestAction,
} from "../types/userRquestTypes";

function getUserRequestGetRequest(): UserRequestAction {
  return {
    type: USER_REQUEST_GET_REQUEST,
  };
}

function getUserRequestGetSuccess(userRequest: UserRequest): UserRequestAction {
  return {
    type: USER_REQUEST_GET_SUCCESS,
    userRequest,
  };
}

function getUserRequestGetFailure(error: string): UserRequestAction {
  return {
    type: USER_REQUEST_GET_FAILURE,
    error,
  };
}

export function getUserRequest(requestId: string) {
  return async (dispatch: Dispatch<UserRequestAction>) => {
    dispatch(getUserRequestGetRequest());
    try {
      const response = await backendProvider.get(`/user-request/${requestId}`);
      const userRequest: UserRequest = {
        content: response.data.content,
      };
      dispatch(getUserRequestGetSuccess(userRequest));
    } catch (error) {
      dispatch(getUserRequestGetFailure(extractError(error)));
    }
  };
}
