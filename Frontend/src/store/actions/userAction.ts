import { Dispatch } from "redux";
import { User } from "../../models/User";
import { backendProvider, extractError } from "../../services/BackendProvider";
import {
  USER_GET_FAILURE,
  USER_GET_REQUEST,
  USER_GET_SUCCESS,
  UserAction,
} from "../types/userTypes";

function getUserRequest(): UserAction {
  return {
    type: USER_GET_REQUEST,
  };
}

function getUserSuccess(user: User): UserAction {
  return {
    type: USER_GET_SUCCESS,
    user,
  };
}

function getUserFailure(error: string): UserAction {
  return {
    type: USER_GET_FAILURE,
    error,
  };
}

export function getUser() {
  return async (dispatch: Dispatch<UserAction>) => {
    dispatch(getUserRequest());
    try {
      const response = await backendProvider.get("/user");
      dispatch(getUserSuccess(response.data));
    } catch (error) {
      dispatch(getUserFailure(extractError(error)));
    }
  };
}
