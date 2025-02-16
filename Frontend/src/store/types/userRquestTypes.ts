import { UserRequest } from "../../models/UserRequest";
import { CommonAction, CommonState } from "./commonTypes";

export const USER_REQUEST_GET_REQUEST = "@USER_REQUEST/GET_REQUEST";
export const USER_REQUEST_GET_SUCCESS = "@USER_REQUEST/GET_SUCCESS";
export const USER_REQUEST_GET_FAILURE = "@USER_REQUEST/GET_FAILURE";

export type UserRequestState = CommonState & {
  userRequest?: UserRequest;
};

type UserRequestGetRequestAction = {
  type: typeof USER_REQUEST_GET_REQUEST;
};

type UserRequestGetSuccessAction = {
  type: typeof USER_REQUEST_GET_SUCCESS;
  userRequest: UserRequest;
};

type UserRequestGetFailureAction = {
  type: typeof USER_REQUEST_GET_FAILURE;
  error: string;
};

export type UserRequestAction =
  | CommonAction
  | UserRequestGetRequestAction
  | UserRequestGetSuccessAction
  | UserRequestGetFailureAction;
