import { Dispatch } from "redux";
import { ChatMessage } from "../../models/ChatMessage";
import { ChatRole } from "../../models/ChatRole";
import { ResponseChoice } from "../../models/ResponseChoice";
import { backendProvider, extractError } from "../../services/BackendProvider";
import {
  CHAT_ADD_MESSAGE_FAILURE,
  CHAT_ADD_MESSAGE_REQUEST,
  CHAT_ADD_MESSAGE_SUCCESS,
  ChatAction,
} from "../types/chatTypes";

function chatAddMessageRequest(message: string): ChatAction {
  return {
    type: CHAT_ADD_MESSAGE_REQUEST,
    message,
  };
}

function chatAddMessageSuccess(message: ChatMessage): ChatAction {
  return {
    type: CHAT_ADD_MESSAGE_SUCCESS,
    message,
  };
}

function chatAddMessageFailure(error: string): ChatAction {
  return {
    type: CHAT_ADD_MESSAGE_FAILURE,
    error,
  };
}

export function addChatMessage(chatRequestId: string, newMessage: string) {
  return async (dispatch: Dispatch<ChatAction>) => {
    dispatch(chatAddMessageRequest(newMessage));
    try {
      const chatRequest = {
        message: newMessage,
        requestId: chatRequestId,
      };
      const response = await backendProvider.post("/chat", chatRequest);
      var responseChoice: ResponseChoice = response.data;
      var chatMessage: ChatMessage = {
        role: ChatRole.ASSISTANT,
        content: responseChoice.message.content,
        context: responseChoice.context,
      };
      dispatch(chatAddMessageSuccess(chatMessage));
    } catch (error) {
      dispatch(chatAddMessageFailure(extractError(error)));
    }
  };
}
