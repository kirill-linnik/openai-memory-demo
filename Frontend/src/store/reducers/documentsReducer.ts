import {
  DOCUMENTS_ADD_FILE_FAILURE,
  DOCUMENTS_ADD_FILE_SUCCESS,
  DOCUMENTS_GET_ALL_FAILURE,
  DOCUMENTS_GET_ALL_REQUEST,
  DOCUMENTS_GET_ALL_SUCCESS,
  DocumentsState,
} from "../types/documentsTypes";
import commonReducer from "./commonReducer";

const initialState: DocumentsState = {};

const documentsReducer = (
  state = initialState,
  action: any
): DocumentsState => {
  switch (action.type) {
    case DOCUMENTS_ADD_FILE_SUCCESS:
      return {
        ...state,
        needsRefresh: true,
      };
    case DOCUMENTS_GET_ALL_REQUEST:
      return {
        ...state,
        needsRefresh: false,
      };
    case DOCUMENTS_GET_ALL_SUCCESS:
      return {
        ...state,
        documents: action.documents,
      };
    case DOCUMENTS_ADD_FILE_FAILURE:
    case DOCUMENTS_GET_ALL_FAILURE:
      return {
        ...state,
        documents: state.documents ? state.documents : [],
        error: action.error,
        needsRefresh: false,
      };
    default:
      return commonReducer(state, initialState, action);
  }
};

export default documentsReducer;
