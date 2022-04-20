# UnrealSumou

![screenShot.png](https://github.com/kayac/UnrealSumou/blob/4cb40c981bb881ad84665d324d258d833a384c38/screenShot.png)

Kayac社内のイベントで開発した相撲ゲームです。
開発の経緯と処理の概要については、[技術ブログの記事](https://techblog.kayac.com/sumo-game-memories)があるのでそちらを参照ください。

## インストール

[releaseページ](https://github.com/kayac/UnrealSumou/releases/tag/v0.0.1)
からwindows用かmac用どちらかをダウンロードしてzipを展開し、
windowsでは.exe、macでは.appを実行してください。

## 遊び方

相手を土俵から落とせば勝ちです。

基本はキーボード操作で、キーボードの左の方が1P、右の方が2Pです。
実際に何を押すとどうなるかは、画面左上の?マークをクリックすると操作説明が出ますので参照ください。

PlayStation4コントローラで操作することもできます。

## 本ソースコードについて

配布しているビルドでは力士のモデルやアニメーション、効果音等が使われていますが、
公開しているこのソースコードではそれらを削除し、
力士の代わりに[ユニティちゃん](https://unity-chan.com/)を使用しております。

効果音の再生に関するコードはコメントアウト状態で残してありますので、
Resources/Sound以下に音声ファイルを配置してファイル名を指定すれば
鳴らすことができます。

また、キャラクターが土俵に埋まった状態でゲームが始まりますが、
これは一部のアニメーションを削除してあるためです。
あらかじめご了承ください。

使用しているUnityのバージョンは2021.3.0です。それより新しければおおよそ動くと思われます。

## 使用アセットのライセンス表示

© Unity Technologies Japan/UCL

https://unity-chan.com/contents/guideline/

