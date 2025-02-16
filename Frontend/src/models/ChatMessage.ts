import { ChatRole } from "./ChatRole";
import { ResponseContext } from "./ResponseContext";

export type ChatMessage = {
  role: ChatRole;
  content: string;
  context?: ResponseContext;
};
