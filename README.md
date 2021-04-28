# MohoFBXImporter
 Version 0.5

# 概要 (これはなに？)
  Mohoという２Dアニメーションツールで作成したSmartWarpアニメーションをUnityにImportする便利Importerです。

#　できること
　Mohoで作ったSmartWarpを使ったアニメーションを同じようにUnityで動かすことができます。
　またMohoの中で動かしたものやBoneを設定したものも持ってくることができます。

# MohoFBXImporterの内容  
* MohoScript
  ・ExporterForUnity.lua  

* Unity Editor拡張
  ・MohoFBXImporter  

#  使用法/インストール方法
※MohoでScriptを動かす準備ができているのなら3)から作業してください。
  よくわからないなという人はそのまま進めてみてください。

* MohoのSetUp
  1) 任意の場所に、mohoのScritを置くフォルダを作成します。  
  ※ここでは仮にここに作ることにします。    
   C:\Users\[UserName]\Documents\moho  

  2) mohoを起動しウィンドウ上部のメニューから  
  Scripts > Install Scripts... を選択  
  先ほど作成したフォルダ↓を選択します。  
   C:\Users\[UserName]\Documents\moho  

  3) フォルダの中にいくつかフォルダが作られます。  
  Moho Pro\scripts\menu  
  この中に↓のScriptをコピーしてください。  
    ExporterForUnity.lua  
    ExporterForUnity.png  

    mohoを起動しなおせばScriptは使えるようになります。  

* 使いかた  
  ・Moho側の作業  
  〇JsonファイルのExport  
  1) Mohoで出力したいファイルを開く  
  2) ToolsのOtherにExporterForUnityのアイコンが出ているのでクリック  
  3) Exportボタンを押す  

  〇FBXファイルのExport  
  MohoのFileメニューから  
  Export>Export FBXを選択する  

  ・Unity側の作業  
  MohoからのFBXがImportされるときに自動で動作します。  
  同じフォルダに出力されたPrefabがImportされたデータとなります。  

  注意点）MohoのJsonファイル、MohoのFBXファイルは同じフォルダに配置されるようにしてください。  

# 再Importの注意点  
  FBXファイルでReimportを行うことで再Importできます。  
  問題が起きた場合、生成されたアニメーションファイル、アニメーションコントローラー、  
  マテリアル、テクスチャ等を削除してからReimportを行ってください  

# 既知の問題  

#  ライセンス

#  contributors
  株式会社IRIAM
