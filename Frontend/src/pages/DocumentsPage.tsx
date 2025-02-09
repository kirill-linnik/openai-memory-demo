import { InboxOutlined, ReloadOutlined } from "@ant-design/icons";
import { message, Table, TableColumnsType, Upload } from "antd";
import { FC, useEffect } from "react";
import { ErrorBlock } from "../components/ErrorBlock";
import { DocumentResponse } from "../models/DocumentResponse";
import { useAppDispatch, useAppSelector } from "../store";
import { addDocument, getAllDocuments } from "../store/actions/documentsAction";

const { Dragger } = Upload;

const DocumentsPage: FC = () => {
  const dispatch = useAppDispatch();
  const documents = useAppSelector((state) => state.documents);

  useEffect(() => {
    if (documents.documents === undefined || documents.needsRefresh) {
      dispatch(getAllDocuments());
    }
  }, [documents.needsRefresh, documents.documents]);

  const columns: TableColumnsType<DocumentResponse> = [
    {
      key: "name",
      dataIndex: "name",
      title: "Name",
      sorter: (a, b) => a.name.localeCompare(b.name),
    },
    {
      key: "contentType",
      dataIndex: "contentType",
      title: "Content type",
      sorter: (a, b) => a.contentType.localeCompare(b.contentType),
    },
    {
      key: "size",
      dataIndex: "size",
      title: "Size",
      sorter: (a, b) => a.size - b.size,
    },
    {
      key: "lastModified",
      render: (_, record) => record.lastModified.format("YYYY-MM-DD HH:mm:ss"),
      title: "Last modified",
      sorter: (a, b) => a.lastModified.diff(b.lastModified),
    },
    {
      key: "status",
      dataIndex: "status",
      title: "Status",
      sorter: (a, b) => a.status.localeCompare(b.status),
    },
  ];

  return (
    <>
      <h1>Documents</h1>
      <ErrorBlock error={documents.error} />
      <Dragger
        name="file"
        multiple={true}
        onChange={(info) => {
          const { status } = info.file;
          if (status !== "uploading") {
            console.log(info.file, info.fileList);
          }
          if (status === "done") {
            message.success(`${info.file.name} file uploaded successfully.`);
          } else if (status === "error") {
            message.error(`${info.file.name} file upload failed.`);
          }
        }}
        beforeUpload={(file) => {
          dispatch(addDocument(file));
          return false;
        }}
      >
        <p className="ant-upload-drag-icon">
          <InboxOutlined />
        </p>
        <p className="ant-upload-text">
          Click or drag file to this area to upload
        </p>
      </Dragger>
      <br />
      <h2>
        Uploaded documents{" "}
        <ReloadOutlined onClick={() => dispatch(getAllDocuments())} />
      </h2>
      <Table
        rowKey={(record) => record.name}
        columns={columns}
        dataSource={documents.documents ? documents.documents : []}
      />
    </>
  );
};

export default DocumentsPage;
