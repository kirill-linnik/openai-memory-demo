import { SupportingContentRecord } from "./SupportingContentRecord";

export type ResponseContext = {
  dataPoints: Array<SupportingContentRecord>;
  thoughts: string;
};
