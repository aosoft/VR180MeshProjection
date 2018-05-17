VR180 Mesh Projection Box Parser
====

Copyright (c) Yasuhiro Taniuchi  

## 目的

VR180 カメラ (Lenovo Mirage Camera) で撮影した動画ファイルに書かれている VR180 用の Metadata を読み込むコードの実装例です。

VR180 カメラは "Mesh Projection Box" という撮影した映像を 3D Mesh にマッピングするための Metadata を記録しています。サンプルコードとして読み込んだ Metadata から Unity の Mesh に変換して可視化する実装もしています。

* 主なファイル構成。
    * Mesh Projection Box のバイナリーデーターを処理可能な状態にパースする。 (MeshProjeectionBox.cs / 本体)
    * MP4 ファイルを解析する。 (BoxHeader.cs)
    * 指定ソースの MeshProjectionBox から Unity の Mesh を生成、設定し、 Video Player を用いて再生する。 (VR180Mesh.cs)
* Unity 2017.4.3f1 で実装しています。
    * MeshProjectionBox.cs, BoxHeader.cs 自体は Unity に依存していません。
    * Unity に依存しているコードももっと古いバージョンの Unity でも動作するはずです。

## サンプルの使い方

"VR180 Video Player" の Video Player コンポーネントの URL に再生したいソース (ローカルファイルに限る) を指定し、 Unity 上で実行してください。

StereoView.shader は VR 時に左右それぞれの映像がレンダリングされるように記述していますので、 XR Setting で VR HMD を使用するようにすれば立体視で見る事もできます。

### サンプルの注意事項

* MeshProjectionBox.cs は仕様書に準じるようにそれなりに実装をしていますが、それ以外は割と適当です。
    * 特に MP4 パーサーは仕様書を見ないで書いているのでもしかしたらうまく読めない可能性もあります。
      読めない場合はコードを修正してください。
    * エラー処理が不十分です。特に起動時のパース処理は失敗する可能性は高いです。

## License

zlib/libpng License

