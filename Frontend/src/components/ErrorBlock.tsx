import { Alert } from "antd";
import { useEffect } from "react";
import { useTranslation } from "react-i18next";
import { useAppDispatch } from "../store";
import { COMMON_REMOVE_ERROR } from "../store/types/commonTypes";

interface IErrorBlockProps {
  error: string | undefined | null;
}

export const ErrorBlock = ({ error }: IErrorBlockProps) => {
  const { t } = useTranslation();
  const dispatch = useAppDispatch();

  const delay: number = 4500;
  useEffect(() => {
    if (error) {
      setTimer(delay);
    }
  }, [error]);

  const setTimer = (delay: number) => {
    setTimeout(() => dispatch({ type: COMMON_REMOVE_ERROR }), delay);
  };

  if (error) {
    return (
      <>
        <Alert
          type="error"
          showIcon
          closable
          message={t("error")}
          description={t(error)}
        />
        <br />
      </>
    );
  } else {
    return <></>;
  }
};
