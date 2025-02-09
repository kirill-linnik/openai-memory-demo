import { ChatMessage } from "./ChatMessage";
import { ResponseContext } from "./ResponseContext";

export type ResponseChoice = {
  message: ChatMessage;
  context: ResponseContext;
};
