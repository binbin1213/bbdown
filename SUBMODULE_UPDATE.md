# 如何更新 BBDown 子模块

本项目依赖 `nilaoda/BBDown` 作为子模块来提供核心功能。为了确保构建的稳定性，子模块的版本是锁定的，不会自动更新。

当您需要将 `BBDown` 核心代码更新到最新版本时，请遵循以下步骤。

## 更新步骤

1.  **进入子模块目录**

    在终端中，使用 `cd` 命令进入项目根目录下的 `BBDown` 文件夹。

    ```bash
    cd BBDown
    ```

2.  **拉取最新代码**

    在 `BBDown` 目录内，执行 `git pull` 命令。此操作会从上游仓库（`nilaoda/BBDown`）拉取最新的代码。

    ```bash
    git pull origin main
    ```

3.  **返回主项目根目录**

    更新完成后，使用 `cd ..` 返回到您的项目主目录。

    ```bash
    cd ..
    ```

4.  **提交子模块版本更新**

    返回主项目后，您会发现 `git status` 显示 `BBDown` 目录有修改。这是正常的，因为它现在指向了一个新的提交记录。

    使用以下命令将这次更新提交到您的主仓库。

    ```bash
    git add BBDown
    git commit -m "chore: 更新 BBDown 子模块到最新版本"
    git push
    ```

完成这些步骤后，您的项目就正式“记住”了 `BBDown` 的新版本。之后所有通过 GitHub Actions 触发的构建都将使用这个最新的核心代码。
