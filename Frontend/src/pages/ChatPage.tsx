import { InfoCircleOutlined, SendOutlined } from "@ant-design/icons";
import { Button, Form, Input, Popover } from "antd";
import { FC, ReactNode, useEffect, useState } from "react";
import ReactMarkdown from "react-markdown";
import { ErrorBlock } from "../components/ErrorBlock";
import { ChatRole } from "../models/ChatRole";
import { generateUUIDv4 } from "../services/UuidGenerator";
import { useAppDispatch, useAppSelector } from "../store";
import { addChatMessage } from "../store/actions/chatActions";
import { getUser } from "../store/actions/userAction";
import { getUserRequest } from "../store/actions/userRequestAction";
import "./ChatStyles.css";

const ChatPage: FC = () => {
  const dispatch = useAppDispatch();
  const chat = useAppSelector((state) => state.chat);
  const user = useAppSelector((state) => state.user);
  const userRequest = useAppSelector((state) => state.userRequest);

  const [chatRequestId] = useState<string>(generateUUIDv4());

  useEffect(() => {
    const interval = setInterval(() => {
      dispatch(getUser());
    }, 500);
    return () => clearInterval(interval);
  }, []);

  useEffect(() => {
    const interval = setInterval(() => {
      dispatch(getUserRequest(chatRequestId));
    }, 500);
    return () => clearInterval(interval);
  }, []);

  const [form] = Form.useForm();

  const submitChatMessage = (values: any) => {
    dispatch(addChatMessage(chatRequestId, values.text));
    form.resetFields();
  };

  const displayChatHistory = (): ReactNode => {
    const chatBubbles = chat.history.map((message, index) => {
      return (
        <div key={index}>
          {message.role === ChatRole.USER ? (
            <div className="outgoing-chats">
              <div className="outgoing-msg">
                <div className="outgoing-chats-msg">
                  <p>{message.content}</p>
                </div>
              </div>
            </div>
          ) : (
            <div className="received-chats">
              <div className="received-msg">
                <div className="received-msg-inbox">
                  <p className="text">
                    <ReactMarkdown>{message.content}</ReactMarkdown>
                    <Popover
                      placement="bottom"
                      content={message.context?.thoughts}
                    >
                      <InfoCircleOutlined />
                    </Popover>
                  </p>
                </div>
              </div>
            </div>
          )}
        </div>
      );
    });
    return (
      <div className="chats">
        <div className="msg-page">{chatBubbles}</div>
      </div>
    );
  };

  return (
    <>
      <ErrorBlock error={chat.error} />
      <div className="container">
        <strong>Chat request ID:</strong> {chatRequestId}
        <br />
        <strong>User profile:</strong> {user.user?.profile}
        <br />
        <strong>User request:</strong> {userRequest.userRequest?.content}
        {displayChatHistory()}
        <div className="msg-bottom">
          <div className="input-group">
            <Form
              name="chatForm"
              initialValues={{
                layout: "inline",
              }}
              form={form}
              layout="inline"
              onFinish={submitChatMessage}
            >
              <Form.Item
                name="text"
                rules={[
                  {
                    required: true,
                  },
                ]}
                style={{ width: "800px" }}
              >
                <Input placeholder="Type a message" />
              </Form.Item>
              <Form.Item>
                <Button type="primary" htmlType="submit">
                  <SendOutlined />
                </Button>
              </Form.Item>
            </Form>
          </div>
        </div>
      </div>
    </>
  );
};

export default ChatPage;
