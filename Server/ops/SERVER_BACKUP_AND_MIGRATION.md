# 凡响智道服务器备份与迁移

## 自动备份

服务器每天凌晨执行 PostgreSQL 备份，备份目录：

```text
/opt/shejiguanli/backups/postgres
```

关键脚本：

```text
/opt/shejiguanli/scripts/backup-postgres.sh
/opt/shejiguanli/scripts/check-backup.sh
/opt/shejiguanli/scripts/restore-postgres.sh
```

推荐定时任务：

```cron
20 3 * * * /opt/shejiguanli/scripts/backup-postgres.sh >/opt/shejiguanli/logs/backup.log 2>&1
40 3 * * * /opt/shejiguanli/scripts/check-backup.sh >/opt/shejiguanli/logs/backup-check.log 2>&1
```

备份会保留最近 60 天，并生成：

```text
/opt/shejiguanli/backups/postgres/latest.sql.gz
/opt/shejiguanli/backups/last-backup-status.txt
```

## 手动检查备份

```bash
/opt/shejiguanli/scripts/check-backup.sh
ls -lh /opt/shejiguanli/backups/postgres
cat /opt/shejiguanli/backups/last-backup-status.txt
```

## 恢复数据库

先确认要恢复的备份文件，再执行：

```bash
/opt/shejiguanli/scripts/restore-postgres.sh /opt/shejiguanli/backups/postgres/postgres-YYYYMMDD-HHMMSS.sql.gz
```

脚本会要求输入 `RESTORE` 才继续，避免误操作。

## 更换服务器

1. 新服务器安装 Docker 和 Docker Compose。
2. 从 GitHub 拉取项目代码。
3. 拷贝旧服务器备份文件到新服务器。
4. 在新服务器恢复 PostgreSQL 备份。
5. 启动 API 服务。
6. 修改客户端 `api-url.txt` 为新服务器地址，重新发布正式版。

如果以后绑定域名，例如 `api.fanxiangzhidao.com`，换服务器时只需要改域名解析，客户端通常不用重新打包。
