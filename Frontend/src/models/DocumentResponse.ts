import { Dayjs } from "dayjs";
import { DocumentProcessingStatus } from "./DocumentProcessingStatus";

export type DocumentResponse = {
  name: string;
  contentType: string;
  size: number;
  lastModified: Dayjs;
  status: DocumentProcessingStatus;
};
