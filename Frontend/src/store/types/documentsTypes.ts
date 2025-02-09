import { DocumentResponse } from "../../models/DocumentResponse";
import { CommonState } from "./commonTypes";

export const DOCUMENTS_ADD_FILE_REQUEST = "@DOCUMENTS/ADD_FILE_REQUEST";
export const DOCUMENTS_ADD_FILE_SUCCESS = "@DOCUMENTS/ADD_FILE_SUCCESS";
export const DOCUMENTS_ADD_FILE_FAILURE = "@DOCUMENTS/ADD_FILE_FAILURE";

export const DOCUMENTS_GET_ALL_REQUEST = "@DOCUMENTS/GET_ALL_REQUEST";
export const DOCUMENTS_GET_ALL_SUCCESS = "@DOCUMENTS/GET_ALL_SUCCESS";
export const DOCUMENTS_GET_ALL_FAILURE = "@DOCUMENTS/GET_ALL_FAILURE";

export type DocumentsState = CommonState & {
  documents?: Array<DocumentResponse>;
};

type DocumentsAddFileRequestAction = {
  type: typeof DOCUMENTS_ADD_FILE_REQUEST;
};

type DocumentsAddFileSuccessAction = {
  type: typeof DOCUMENTS_ADD_FILE_SUCCESS;
};

type DocumentsAddFileFailureAction = {
  type: typeof DOCUMENTS_ADD_FILE_FAILURE;
  error: string;
};

type DocumentsGetAllRequestAction = {
  type: typeof DOCUMENTS_GET_ALL_REQUEST;
};

type DocumentsGetAllSuccessAction = {
  type: typeof DOCUMENTS_GET_ALL_SUCCESS;
  documents: Array<DocumentResponse>;
};

type DocumentsGetAllFailureAction = {
  type: typeof DOCUMENTS_GET_ALL_FAILURE;
  error: string;
};

export type DocumentsAction =
  | DocumentsAddFileRequestAction
  | DocumentsAddFileSuccessAction
  | DocumentsAddFileFailureAction
  | DocumentsGetAllRequestAction
  | DocumentsGetAllSuccessAction
  | DocumentsGetAllFailureAction;
