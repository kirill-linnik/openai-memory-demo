import {
  COMMON_REMOVE_ERROR,
  COMMON_REMOVE_INFO,
  CommonState,
} from "../types/commonTypes";

const commonReducer = (
  state: CommonState,
  initialState: CommonState,
  action: any
): any => {
  switch (action.type) {
    case COMMON_REMOVE_ERROR:
      return {
        ...state,
        error: undefined,
      };
    case COMMON_REMOVE_INFO:
      return {
        ...state,
        info: undefined,
      };
    default:
      return state;
  }
};

export default commonReducer;
