import { User } from "../../models/User";
import { CommonAction, CommonState } from "./commonTypes";

export const USER_GET_REQUEST = "@USER/GET_REQUEST";
export const USER_GET_SUCCESS = "@USER/GET_SUCCESS";
export const USER_GET_FAILURE = "@USER/GET_FAILURE";

export type UserState = CommonState & {
  user?: User;
};

type UserGetRequestAction = {
  type: typeof USER_GET_REQUEST;
};

type UserGetSuccessAction = {
  type: typeof USER_GET_SUCCESS;
  user: User;
};

type UserGetFailureAction = {
  type: typeof USER_GET_FAILURE;
  error: string;
};

export type UserAction =
  | CommonAction
  | UserGetRequestAction
  | UserGetSuccessAction
  | UserGetFailureAction;
