# PotisanWmiLib

C#用のWMIの薄いラッパークラスライブラリです。

## 使用方法

最初に`WmiUtility.InitializeComSecurity`静的メソッドを呼び出してください。以降は`WbemLocator`クラスからWMIの機能を使用できます。

### WinFormsで使用する場合の注意点

WinFormsプロジェクトで使用する場合は次の方法でvshost.exeを無効化してください。無効化しない場合、WMI処理に必要なセキュリティ設定 (`WmiUtility.InitializeComSecurity`静的メソッド)でエラーが発生します。`WmiUtility.InitializeComSecurity`静的メソッドを呼び出さない場合、クラス情報は取得できますがインスタンス情報は取得できません。

- Visual Studio 2022の場合
  - プロジェクトのプロパティ→デバッグ→全般→デバッグ起動プロファイルUIを開く→「ホットリロードを有効にする」の「実行中のアプリケーションにコードの変更を適用します。」のチェックを外す。

確認時点ではコンソールアプリケーションはこの設定が不要です。

### 同梱物

WmiViewはPotisanWmiLibのWinFormsにおける機能を確認するためのWindowsフォームアプリケーションです。
