import { Layout, Menu } from "antd";
import { FC, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";

const { Header } = Layout;

export const HeaderComponent: FC = () => {
  const location = useLocation();
  const navigate = useNavigate();

  let pathname = location.pathname.substring(1);
  if (pathname.indexOf("/") !== -1) {
    pathname = pathname.substring(0, pathname.indexOf("/"));
  }
  const [currentPage, setCurrentPage] = useState<string>(pathname);

  return (
    <Header style={{ display: "flex", alignItems: "center" }}>
      <div className="demo-logo" />
      <Menu
        theme="dark"
        mode="horizontal"
        defaultSelectedKeys={["chat"]}
        items={[
          { key: "", label: "chat" },
          {
            key: "documents",
            label: "documents",
          },
        ]}
        style={{ flex: 1, minWidth: 0 }}
        onClick={(e) => {
          const key = e.key;
          setCurrentPage(key);
          navigate(key);
        }}
        selectedKeys={[currentPage]}
      />
    </Header>
  );
};
