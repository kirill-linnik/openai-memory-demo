import "@ant-design/v5-patch-for-react-19";
import { Layout, theme } from "antd";
import { FC, lazy } from "react";
import { BrowserRouter, Route, Routes } from "react-router-dom";
import "./App.css";
import { HeaderComponent } from "./components/HeaderComponent";

const { Header, Content, Footer } = Layout;

const ChatPage = lazy(() => import("./pages/ChatPage"));
const DocumentsPage = lazy(() => import("./pages/DocumentsPage"));
const NotFoundPage = lazy(() => import("./pages/NotFoundPage"));

const App: FC = () => {
  const {
    token: { colorBgContainer, borderRadiusLG },
  } = theme.useToken();

  return (
    <>
      <BrowserRouter>
        <Layout>
          <HeaderComponent />
          <Content style={{ padding: "0 48px" }}>
            <div
              style={{
                background: colorBgContainer,
                minHeight: "calc(100vh - 64px)",
                padding: 24,
                borderRadius: borderRadiusLG,
              }}
            >
              <div className="page-content">
                <Routes>
                  <Route path="/" element={<ChatPage />} />
                  <Route path="/documents" element={<DocumentsPage />} />
                  <Route path="*" element={<NotFoundPage />} />
                </Routes>
              </div>
            </div>
          </Content>
        </Layout>
      </BrowserRouter>
    </>
  );
};

export default App;
