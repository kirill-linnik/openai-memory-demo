import { RcFile } from "antd/es/upload";
import dayjs from "dayjs";
import { Dispatch } from "redux";
import { DocumentProcessingStatus } from "../../models/DocumentProcessingStatus";
import { DocumentResponse } from "../../models/DocumentResponse";
import { backendProvider, extractError } from "../../services/BackendProvider";
import {
  DOCUMENTS_ADD_FILE_FAILURE,
  DOCUMENTS_ADD_FILE_REQUEST,
  DOCUMENTS_ADD_FILE_SUCCESS,
  DOCUMENTS_GET_ALL_FAILURE,
  DOCUMENTS_GET_ALL_REQUEST,
  DOCUMENTS_GET_ALL_SUCCESS,
  DocumentsAction,
} from "../types/documentsTypes";

function getAllDocumentsRequest(): DocumentsAction {
  return {
    type: DOCUMENTS_GET_ALL_REQUEST,
  };
}

function getAllDocumentsSuccess(
  documents: Array<DocumentResponse>
): DocumentsAction {
  return {
    type: DOCUMENTS_GET_ALL_SUCCESS,
    documents,
  };
}

function getAllDocumentsFailure(error: string): DocumentsAction {
  return {
    type: DOCUMENTS_GET_ALL_FAILURE,
    error,
  };
}

function addDocumentRequest(): DocumentsAction {
  return {
    type: DOCUMENTS_ADD_FILE_REQUEST,
  };
}

function addDocumentSuccess(): DocumentsAction {
  return {
    type: DOCUMENTS_ADD_FILE_SUCCESS,
  };
}

function addDocumentFailure(error: string): DocumentsAction {
  return {
    type: DOCUMENTS_ADD_FILE_FAILURE,
    error,
  };
}

export function getAllDocuments() {
  return async (dispatch: Dispatch<DocumentsAction>) => {
    dispatch(getAllDocumentsRequest());
    try {
      const response = await backendProvider.get("/documents/all");
      dispatch(
        getAllDocumentsSuccess(convertToDocumentResponse(response.data))
      );
    } catch (error) {
      dispatch(getAllDocumentsFailure(extractError(error)));
    }
  };
}

export function addDocument(file: RcFile) {
  return async (dispatch: Dispatch<DocumentsAction>) => {
    dispatch(addDocumentRequest());
    try {
      const formData = new FormData();
      formData.append("files", file);
      await backendProvider.post("/documents", formData, {
        headers: {
          "Content-Type": "multipart/form-data",
        },
      });
      dispatch(addDocumentSuccess());
    } catch (error) {
      dispatch(addDocumentFailure(extractError(error)));
    }
  };
}

function convertToDocumentResponse(data: any): Array<DocumentResponse> {
  const documents: Array<DocumentResponse> = [];
  data.forEach((item: any) => {
    const document: DocumentResponse = {
      ...item,
      lastModified: dayjs(item.lastModified),
      status:
        DocumentProcessingStatus[
          item.status as keyof typeof DocumentProcessingStatus
        ],
    };
    documents.push(document);
  });
  return documents;
}
