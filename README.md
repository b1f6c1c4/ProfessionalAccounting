# ProfessionalAccounting

> 使用借贷记帐法的记账软件

[![Appveyor Build](https://img.shields.io/appveyor/build/b1f6c1c4/ProfessionalAccounting?style=flat-square)](https://ci.appveyor.com/project/b1f6c1c4/professionalaccounting)
[![Appveyor Tests](https://img.shields.io/appveyor/tests/b1f6c1c4/ProfessionalAccounting?style=flat-square)](https://ci.appveyor.com/project/b1f6c1c4/professionalaccounting/build/tests)

## 简介

- 基于[借贷记账法](https://en.wikipedia.org/wiki/Double-entry_bookkeeping)
- C/S架构
- 使用DSL（Domain Specfic Langauge）来增删改查
- 后端
    - 单体架构，业务逻辑使用`C# 7.3`编写
    - 数据库采用MongoDB
    - 使用`.NET Framework v4.7.2`平台编译
    - 使用docker部署，其中采用`mono`执行编译好的程序
- 前端
    - 由于逻辑非常简单，前端部分没有采用任何框架
    - （其实只有一个编辑器和一个命令框）

## 安装与配置

### 准备

首先准备一台Linux服务器，安装以下软件：
- docker，推荐采用[get.docker.com](https://get.docker.com/)
- openssl（请自行Google安装方法）

### 配置记账系统功能

为了方便地从GitHub下载单独文件（而不用下载整个repo的整个历史），
此处推荐大家使用[git-get](https://github.com/b1f6c1c4/git-get)来下载。

1. ssh登录*服务器*
1. 下载`docker-compose.yml`文件：
    ```bash
    git get b1f6c1c4/ProfessionalAccounting -- docker-compose.yml
    ```
1. 下载示例配置文件夹，放在`/data/accounting/config.d/`：
    ```bash
    mkdir -p /data/accounting
    git get -o /data/accounting/config.d b1f6c1c4/ProfessionalAccounting -- example/config.d/
    ```
1. 修改必须修改的配置文件：
    - `BaseCurrency.xml` - 记账本位币
    - `Symbol.xml` - 货币符号表
    - `Titles.xml` - 记账科目列表
    - `Carry.xml` - 期末结转规则
    - `Exchange.xml` - 汇率查询API（[fixer.io](https://fixer.io)）的配置
1. 修改可选的配置文件：
    - `Abbr.xml` - 登记新记账凭证时使用的缩写列表
    - `Cash.xml` - 现金流插件相关配置
    - `Composite.xml` - 常用检索式列表
    - `Util.xml` - 快速登记记账凭证插件的配置

### 配置服务器和客户端x509证书

1. 将服务器证书和私钥（`server.crt`，`server.key`）放在服务器的`/data/accounting/certs`目录下
    1. 如果你没有服务器证书，推荐使用[acme.sh](https://github.com/acmesh-official/acme.sh/wiki/%E8%AF%B4%E6%98%8E)来免费获得一个
1. 在服务器上创建dhparams：
    ```bash
    openssl dhparam -out /data/accounting/certs/dhparam.pem 2048
    ```
1. 创建自签名的客户端证书和私钥：
   （注意：如果客户端机器未安装`openssl`，则在服务器上创建客户端的证书和私钥，并把加密后的私钥`client.p12`传回客户端）
    1. **在客户端机器上**创建证书和私钥：
    ```bash
    openssl req -x509 -newkey rsa:2048 -keyout key.pem -out cert.crt -days 1825 -nodes
    chmod 0400 key.pem
    ```
    1. 将证书上传服务器：
    ```bash
    scp ./cert.crt <server>:/data/accounting/certs/client.crt
    ```
    1. 将证书和私钥封装成`.p12`格式，删除原来的`.pem`文件：
    （这一步的目的是方便在客户端上安装证书）
    ```bash
    openssl pkcs12 -export -inkey key.pem -in cert.crt -out client.p12 && rm -f key.pem
    ```
1. 在客户端上安装证书：
    1. 恕不赘述，请自行Google `install p12 certificate on XXX`（`XXX`=Linux/FreeBSD/Windows/MacOS/iOS/iPadOS/...）

## 基本使用方法

### 记账

1. 在客户端上使用浏览器访问服务器的18080端口：`https://<server>:18080/`
1. 可以看到用户界面分为两部分：上面的只有一行的命令框和下面的编辑器。

## 开发

TODO

## 许可

TODO
