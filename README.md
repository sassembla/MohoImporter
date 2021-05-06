# MohoFBXImporter
 Version 0.5
  
![run](https://user-images.githubusercontent.com/944441/117318206-29eb2a00-aec5-11eb-8dab-8a20db30676c.mp4)

# 概要  
 Mohoで作成したSmartWarpアニメーションをUnityにImportすることができます。  

# MohoFBXImporterの内容  
* MohoScript
  ・ExporterForUnity.lua  

* Unity Editor拡張
  ・MohoFBXImporter  


# 利用準備  
※Scriptを配置する準備ができているのなら3)から作業してください。

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
  ・Macではパーツのプライオリティが荒れてしまいます。Windowsでも同様の問題が起きています。  

