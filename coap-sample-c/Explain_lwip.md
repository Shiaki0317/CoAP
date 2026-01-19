# ソースコード説明 (Lwip を利用)

説明対象は以下の 2 つのサンプルコードとする。

- [sample_client](./sample_client/sample_client.c)
- [sample_server](./sample_server/sample_server.c)

## 動作確認手順

動作確認時には上記対象のコードをビルドを行い、実行する。
インストール時には libcoap のビルドが必要となるため、[インストール手順](https://libcoap.net/install.html)を参考にしてビルド及びインストール作業まで完了させる。
インストール完了後には、以下のビルドコマンドにて実行する。

- `gcc -Wall -Wextra -std=c11 -g -O0 -I/usr/local/include -lcoap-3 -c ./sample_client/sample_client.c -o sample_client/sample_client`
- `gcc -Wall -Wextra -std=c11 -g -I/usr/local/include ./sample_server/sample_server.c -o ./sample_server/sample_server -lcoap-3`

ビルド完了後、以下の順序にて実行し、動作確認を行う。

1. 新規でターミナルを起動
2. コマンド`./sample_server/sample_server`を実行
3. 以下のコマンドのいずれかを実行
   - `./sample_client/sample_client /hello` : ローカルの CoAP サーバに URI"/hello"に CoAP リクエスト送信
   - `./sample_client/sample_client /hello/my` : ローカルの CoAP サーバに URI"/hello/mt"に CoAP リクエスト送信
   - `./sample_client/sample_client` : CoAP サーバ"coap.me"に URI"/path/sub1"(COAP_CLIENT_URI の設定に依存)に CoAP リクエスト送信
4. "sample_client"を実行したコマンドラインにてリクエスト送信内容・レスポンス受信結果を表示し、ローカルサーバにて CoAP リクエストを受信した場合にはリクエスト受信内容・レスポンス送信内容を表示する。

## 用語定義

| 用語         | 説明                                                                                                                                |
| ------------ | ----------------------------------------------------------------------------------------------------------------------------------- |
| PDU          | Protocol Data Unit の略称であり，CoAP のメッセージフォーマットになる                                                                |
| コンテキスト | 特定の通信処理に必要な環境情報（ユーザー ID、ロケール、一時変数など）                                                               |
| セッション   | 通信の「始まりから終わりまでの一連の期間・区切り」（時間的なつながり）                                                              |
| スキーム     | URL の先頭にある「http://」のように、「どのような方法（プロトコル）でデータ通信を行うか」を示す識別子                               |
| Leisure Time | マルチキャスト通信（Group Communication）にてネットワークが一度に大量の応答で溢れかえる（輻輳する）のを防ぐための遅延時間パラメータ |

## Client サンプルコード

```c
/* minimal CoAP client
 *
 * Copyright (C) 2018-2024 Olaf Bergmann <bergmann@tzi.org>
 */

/*
 * The client can be run as
 *   ./client
 * in which case the CoAP URI to make the request against is defined
 * by COAP_CLIENT_URI, or
 *   ./client CoAP_Uri
 * where CoAP_Uri is a correctly formatted CoAP URI.
 */

#include <string.h>
#include <stdlib.h>
#include <stdio.h>

#include <coap3/coap.h>

#ifndef COAP_CLIENT_URI
// #define COAP_CLIENT_URI "coap://coap.me/hello"
#define COAP_CLIENT_URI "coap://coap.me/path/sub1"
#endif

#define BUFSIZE 100

static int have_response = 0;

coap_response_t response_handler_sample(coap_session_t *session, const coap_pdu_t *sent, const coap_pdu_t *received, const coap_mid_t mid) {
  size_t len;
  const uint8_t *databuf;
  size_t offset;
  size_t total;

  have_response = 1;
  coap_show_pdu(COAP_LOG_WARN, received);
  if (coap_get_data_large(received, &len, &databuf, &offset, &total)) {
    fwrite(databuf, 1, len, stdout);
    fwrite("\n", 1, 1, stdout);
  }
  return COAP_RESPONSE_OK;
}

int main(int argc, char *argv[]) {
  coap_context_t *ctx = NULL;
  coap_session_t *session = NULL;
  coap_optlist_t *optlist = NULL;
  coap_address_t dst;
  coap_addr_info_t *addr_info = NULL;
  coap_pdu_t *pdu = NULL;
  int result = EXIT_FAILURE;;
  int len;
  int res;
  unsigned int wait_ms;
  coap_uri_t uri;
  const char *coap_uri = COAP_CLIENT_URI;
  int is_mcast;
  unsigned char scratch[BUFSIZE];

  /* Support run-time defining of CoAP URIs */
  if (argc > 1) {
    coap_uri = argv[1];
  }

  /* Initialize libcoap library */
  coap_startup();

  /* Set logging level */
  coap_set_log_level(COAP_LOG_WARN);

  /* Parse the URI */
  len = coap_split_uri((const unsigned char *)coap_uri, strlen(coap_uri), &uri);
  if (len != 0) {
    coap_log_warn("Failed to parse uri %s\n", coap_uri);
    goto finish;
  }

  /* resolve destination address where server should be sent */
  len = 0;

  addr_info = coap_resolve_address_info(&uri.host, uri.port, uri.port, uri.port,
                                        uri.port, AF_UNSPEC, (1 << uri.scheme),
                                        COAP_RESOLVE_TYPE_REMOTE);
  if (addr_info) {
      len = 1;
      dst = addr_info->addr;
  }

  coap_free_address_info(addr_info);

  if (len <= 0) {
      coap_log_warn("Failed to resolve address %*.*s\n", (int)uri.host.length,
                  (int)uri.host.length, (const char *)uri.host.s);
      goto finish;
  }
  is_mcast = coap_is_mcast(&dst);

  /* create CoAP context and a client session */
  if (!(ctx = coap_new_context(NULL))) {
    coap_log_emerg("cannot create libcoap context\n");
    goto finish;
  }
  /* Support large responses */
  coap_context_set_block_mode(ctx, COAP_BLOCK_USE_LIBCOAP | COAP_BLOCK_SINGLE_BODY);

  if (uri.scheme == COAP_URI_SCHEME_COAP) {
    session = coap_new_client_session(ctx, NULL, &dst,
                                      COAP_PROTO_UDP);
  } else if (uri.scheme == COAP_URI_SCHEME_COAP_TCP) {
    session = coap_new_client_session(ctx, NULL, &dst,
                                      COAP_PROTO_TCP);
  }
  if (!session) {
    coap_log_emerg("cannot create client session\n");
    goto finish;
  }

  /* coap_register_response_handler(ctx, response_handler); */
  coap_register_response_handler(ctx, response_handler_sample);
  /* construct CoAP message */
  pdu = coap_pdu_init(is_mcast ? COAP_MESSAGE_NON : COAP_MESSAGE_CON,
                      COAP_REQUEST_CODE_GET,
                      coap_new_message_id(session),
                      coap_session_max_pdu_size(session));
  if (!pdu) {
    coap_log_emerg("cannot create PDU\n");
    goto finish;
  }

  /* Add option list (which will be sorted) to the PDU */
  len = coap_uri_into_options(&uri, &dst, &optlist, 1, scratch, sizeof(scratch));
  if (len) {
    coap_log_warn("Failed to create options\n");
    goto finish;
  }

  if (optlist) {
    res = coap_add_optlist_pdu(pdu, &optlist);
    if (res != 1) {
      coap_log_warn("Failed to add options to PDU\n");
      goto finish;
    }
  }

  coap_show_pdu(COAP_LOG_WARN, pdu);

  /* and send the PDU */
  if (coap_send(session, pdu) == COAP_INVALID_MID) {
    coap_log_err("cannot send CoAP pdu\n");
    goto finish;
  }

  wait_ms = (coap_session_get_default_leisure(session).integer_part + 1) * 1000;

  while (have_response == 0 || is_mcast) {
    res = coap_io_process(ctx, 1000);
    if (res >= 0) {
      if (wait_ms > 0) {
        if ((unsigned)res >= wait_ms) {
          fprintf(stdout, "timeout\n");
          break;
        } else {
          wait_ms -= res;
        }
      }
    }
  }

  result = EXIT_SUCCESS;

finish:
  coap_delete_optlist(optlist);
  coap_session_release(session);
  coap_free_context(ctx);
  coap_cleanup();

  return result;
}
```

### Client サンプルコード内容説明

サンプルコードを例に挙げて，ソースコードを説明する

1. 変数宣言

   ```c
   coap_context_t *ctx = NULL;              // CoAPスタックのグローバル状態
   coap_session_t *session = NULL;          // クライアント or サーバーに接続できる仮想セッションの抽象化
   coap_optlist_t *optlist = NULL;          // インストールする CoAP オプションの連鎖リストの表現
   coap_address_t dst;                      // 多目的アドレス抽象化
   coap_addr_info_t *addr_info;             // アドレス解決用のデータ
   coap_pdu_t *pdu = NULL;                  // CoAP送信データ
   int result = EXIT_FAILURE;               // main関数の動作結果
   int len;                                 //
   int res;                                 //
   unsigned int wait_ms;                    // msec単位の待機時間
   coap_uri_t uri;                          // 解析されたURIの表現
   const char *coap_uri = COAP_CLIENT_URI;  // CoAP用URIデータ(文字列)
   int is_mcast;                            // マルチキャスト判定用フラグ
   unsigned char scratch[BUFSIZE];          //
   ```

2. 引数チェック

   引数チェックとして最初の引数が CoAP で送信する URI として"coap_uri"に格納する．

   ```c
   /* Support run-time defining of CoAP URIs */
   if (argc > 1) {
       coap_uri = argv[1];
   }
   ```

3. CoAP ライブラリ・ログレベル設定の初期化

   "coap_startup"関数は coap\_\*() 関数を呼び出す前に呼び出す必要があり，他のミューテックス・乱数生成器・クロック・TLS ライブラリなどの初期化に使用される．

   また，"coap_log"関数呼び出しによる出力の現在のログレベルを設定するために使用される．
   "coap_log"のレベル定義が現在のログレベル以下の場合にのみ，出力が記録される．

   ```c
   /* Initialize libcoap library */
   coap_startup();

   /* Set logging level */
   coap_set_log_level(COAP_LOG_WARN);
   ```

4. URI パース処理

   "coap_uri"に設定された URI をパースして，ホスト・ポート番号・パス・クエリに分割してデータを"uri"に格納する．
   パースの成功時に"len"に"0"をセットする．
   もし"len"が"0"でない場合にはログに警告を出力し，リソース解放の処理を実行する．

   ```c
   /* Parse the URI */
   len = coap_split_uri((const unsigned char *)coap_uri, strlen(coap_uri), &uri);
   if (len != 0) {
       coap_log_warn("Failed to parse uri %s\n", coap_uri);
       goto finish;
   }
   ```

   "uri"のデータ型は"coap_uri_t"構造体となっており，その内部構造は以下となる．

   | データ型               | 変数名 | 説明                                                         |
   | ---------------------- | ------ | ------------------------------------------------------------ |
   | coap_str_const_t       | host   | URI のホスト                                                 |
   | uint16_t               | port   | URI ポート番号                                               |
   | coap_str_const_t       | path   | URI のパス                                                   |
   | coap_str_const_t       | query  | URI のクエリ                                                 |
   | enum coap_uri_scheme_t | scheme | URI のスキーム (例:http://...の場合は http がスキームとなる) |

5. サーバーの送信先アドレスの解決・マルチキャスト判定

   "coap_resolve_address_info"関数によりホスト名から IP アドレス(IPv4 or IPv6)を変数"addr_info"に格納し，dst にコピーし，不要となったリソース"addr_info"を解放する．
   正常にアドレス解決ができなかった場合には警告のログを出力し，リソース解放の処理を行う．
   正常にアドレス解決ができた場合には，"coap_is_mcast"関数にて IP アドレスよりマルチキャストの判定を実施する．

   ```c
   /* resolve destination address where server should be sent */
   coap_addr_info_t *addr_info;
   addr_info = coap_resolve_address_info(&uri.host, uri.port, uri.port, uri.port,
                                         uri.port, AF_UNSPEC, (1 << uri.scheme),
                                         COAP_RESOLVE_TYPE_REMOTE);
   if (addr_info) {
       len = 1;
       dst = addr_info->addr;
   }

   coap_free_address_info(addr_info);

   if (len <= 0) {
       coap_log_warn("Failed to resolve address %*.*s\n", (int)uri.host.length,
                   (int)uri.host.length, (const char *)uri.host.s);
       goto finish;
   }
   is_mcast = coap_is_mcast(&dst);
   ```

   アドレス解決用に格納されたデータ型として構造体"coap_addr_info_t"が定義されており，構造体は以下の要素から成立している．

   | データ型                   | 変数名 | 説明                    |
   | -------------------------- | ------ | ----------------------- |
   | struct coap_addr_info_t \* | next   | Next entry in the chain |
   | coap_uri_scheme_t          | scheme | CoAP scheme to use      |
   | coap_proto_t               | proto  | CoAP protocol to use    |
   | coap_address_t             | addr   | CoAP 用のアドレス情報   |

6. CoAP 用コンテキスト・セッションの生成

   "coap_new_context"関数は，CoAP リソース，エンドポイント，セッション情報をすべて保持するために使用される新しいコンテキストを作成する．
   コンテキスト作成に失敗した場合には，エラーログを出力し，リソースの解放を行う．

   コンテキストの生成に成功した場合には，コンテキストに送信時のブロック設定を行い，セッションを生成する．
   このとき，URI のスキームに応じて TCP もしくは UDP のセッションを生成する．
   セッションの生成に失敗した場合には，エラーログを出力し，リソースの解放を行う．

   ```c
   /* create CoAP context and a client session */
   if (!(ctx = coap_new_context(NULL))) {
      coap_log_emerg("cannot create libcoap context\n");
      goto finish;
   }

   /* Support large responses */
   coap_context_set_block_mode(ctx, COAP_BLOCK_USE_LIBCOAP | COAP_BLOCK_SINGLE_BODY);

   if (uri.scheme == COAP_URI_SCHEME_COAP) {
       session = coap_new_client_session(ctx, NULL, &dst, COAP_PROTO_UDP);
   } else if (uri.scheme == COAP_URI_SCHEME_COAP_TCP) {
       session = coap_new_client_session(ctx, NULL, &dst, COAP_PROTO_TCP);
   }
   if (!session) {
       coap_log_emerg("cannot create client session\n");
       goto finish;
   }

   ```

7. イベントハンドラの登録

   "coap_register_response_handler"関数 は、コンテキストに関連付けられたトラフィックのリクエスト応答コールバックハンドラを登録するクライアント側関数になる。アプリケーションは、このハンドラを使用して、予期しない応答があった場合に RST パケットを送信するなど、あらゆる応答パケットを処理できる。ハンドラが NULL の場合、ハンドラは登録解除される。

   ```c
   /* coap_register_response_handler(ctx, response_handler); */
   coap_register_response_handler(ctx, response_handler_sample);
   ```

   以下が登録するイベントハンドラの処理となり，レスポンス受信時にコールバック関数として以下の順序で実行される．

   1. "coap_show_pdu" 関数は pdu をデコードし、ログレベルに応じて適切な出力を行う．出力先は "coap_set_show_pdu_output" 関数 に依存する．
   2. "coap_get_data_large" 関数は、受信データに関する PDU 情報を抽象化するために使用され、length を利用可能なデータの長さ、data をデータの位置へのポインタ、offset をこのデータブロックの開始位置、total をデータの総量で更新する．block_mode に COAP_BLOCK_SINGLE_BODY が含まれる場合、offset は常に 0 となる．「offset length == total」の場合、ボディのすべてのデータが受信されている．
   3. "coap_get_data_large" 関数が成功した場合には，コンソールにバッファに保存した文字列を出力する．

   ```c
   coap_response_t response_handler_sample(coap_session_t *session, const coap_pdu_t *sent, const coap_pdu_t *received, const coap_mid_t mid) {
       size_t len;
       const uint8_t *databuf;
       size_t offset;
       size_t total;

       have_response = 1;
       coap_show_pdu(COAP_LOG_WARN, received);
       if (coap_get_data_large(received, &len, &databuf, &offset, &total)) {
           fwrite(databuf, 1, len, stdout);
           fwrite("\n", 1, 1, stdout);
       }
       return COAP_RESPONSE_OK;
   };
   ```

8. CoAP メッセージの生成

   以下の内容を設定し，送信する PDU(メッセージフォーマット)を生成する．そして，データが設定されていない場合にはエラーログを出力し，解放処理を実行する．

   - タイプ設定 : coap_pdu_type_t (CON, NON, ACK, RST)
   - CoAP 要求・応答コード : coap_pdu_code_t (GET・POST・PUT などの要求コード，応答コード)
   - メッセージ ID : uint16_t (利用するセッションごとに "coap_new_message_id"関数でメッセージ ID を新規作成)
   - メッセージの最大許容バイト数 : size_t ("coap_session_max_pdu_size"関数によりクライアントエンドポイントのセッションのデータの最大 MTU サイズを取得する)

   ```c
   /* construct CoAP message */
   pdu = coap_pdu_init(is_mcast ? COAP_MESSAGE_NON : COAP_MESSAGE_CON,
                       COAP_REQUEST_CODE_GET,
                       coap_new_message_id(session),
                       coap_session_max_pdu_size(session));
   if (!pdu) {
       coap_log_emerg("cannot create PDU\n");
       goto finish;
   }
   ```

9. オプション設定

   URI 文字列から CoAP オプション列を生成し，PDU に設定する．

   1. "coap_uri_into_options"関数にて変数"uri"より optlist に生成したオプション一覧を格納する．ただし，変数"dst"が"uri.host"と一致しない場合にはオプションとして"URI-Host"のオプションを追加する．このときのオプションの値は"uri.host"になる．また，scratch・sizeof(scratch)は作業領域になるため， path や query を十分に格納できる程度のサイズを確保しておく．
   2. "coap_add_optlist_pdu"関数は引数にて設定したオプションリストを PDU に設定する．

   ```c
   /* Add option list (which will be sorted) to the PDU */
   len = coap_uri_into_options(&uri, &dst, &optlist, 1, scratch, sizeof(scratch));
   if (len) {
       coap_log_warn("Failed to create options\n");
       goto finish;
   }

   if (optlist) {
       res = coap_add_optlist_pdu(pdu, &optlist);
       if (res != 1) {
           coap_log_warn("Failed to add options to PDU\n");
           goto finish;
       }
   }

   coap_show_pdu(COAP_LOG_WARN, pdu);
   ```

   ここでは設定していないが，トークンを設定する場合には"coap_add_token"関数を用いる．
   この"coap_add_token"関数を用いる場合には"coap_add_optlist_pdu"関数の実行前に行う．

10. CoAP データの送信

    CoAP の送信処理として以下の順序にて実行する．

    1. "coap_send"関数にて生成したメッセージ"pdu"を CoAP で送信処理を実行し，送信処理に失敗した場合はエラーログを出力しリソース解放処理を行う．
    2. セッションの"Leisure Time"に 1 を加えた値を待機時間として設定する．
    3. "coap_io_process"関数にて IO 処理(指定のタイムアウト時間(1000 msec)以内にレスポンス受信)を実行し，

    ```c
    /* and send the PDU */
    if (coap_send(session, pdu) == COAP_INVALID_MID) {
        coap_log_err("cannot send CoAP pdu\n");
        goto finish;
    }

    wait_ms = (coap_session_get_default_leisure(session).integer_part + 1) * 1000;

    while (have_response == 0 || is_mcast) {
        res = coap_io_process(ctx, 1000);
        if (res >= 0) {
            if (wait_ms > 0) {
                if ((unsigned)res >= wait_ms) {
                    fprintf(stdout, "timeout\n");
                    break;
                } else {
                    wait_ms -= res;
                }
            }
        }
    }
    ```

11. 終了時のデータ返却

    正常にクライアント処理が完了したことを main 関数より OS に返す．

    ```c
    result = EXIT_SUCCESS;
    ```

12. CoAP クライアント用の確保データの解放

    データを開放する上での手続きは以下の手順にて実施する．
    そのとき，確保した順番とは逆の手順にて実施される．

    1. オプションリスト
    2. セッションで確保したリソースの解放
    3. コンテキスト用に確保したリソースの解放
    4. ライブラリの初期化にて確保したリソースの解放

    ```c
    finish:
        coap_delete_optlist(optlist);
        coap_session_release(session);
        coap_free_context(ctx);
        coap_cleanup();

    return result;
    ```

## Server サンプルコード

```c
/* minimal CoAP server
 *
 * Copyright (C) 2018-2024 Olaf Bergmann <bergmann@tzi.org>
 */

#include <string.h>
#include <stdlib.h>
#include <stdio.h>
#include <stdbool.h>

#include <coap3/coap.h>

/*
 * This server listens to Unicast CoAP traffic coming in on port 5683 and handles it
 * as appropriate.
 *
 * If support for multicast traffic is not required, comment out the COAP_LISTEN_MCAST_IPV*
 * definitions.
 */

#define COAP_LISTEN_UCAST_IP "::"

#define COAP_LISTEN_MCAST_IPV4 "224.0.1.187"
#define COAP_LISTEN_MCAST_IPV6 "ff02::fd"

void hello_handler(coap_resource_t *resource,coap_session_t *session, const coap_pdu_t *request, const coap_string_t *query, coap_pdu_t *response) {
  coap_show_pdu(COAP_LOG_WARN, request);
  coap_pdu_set_code(response, COAP_RESPONSE_CODE_CONTENT);
  coap_add_data(response, 5,
                (const uint8_t *)"world");
  coap_show_pdu(COAP_LOG_WARN, response);
}

void my_hello_handler(coap_resource_t *resource,coap_session_t *session, const coap_pdu_t *request, const coap_string_t *query, coap_pdu_t *response) {
  coap_show_pdu(COAP_LOG_WARN, request);
  coap_pdu_set_code(response, COAP_RESPONSE_CODE_CONTENT);
  coap_add_data(response, 8, (const uint8_t *)"my world");
  coap_show_pdu(COAP_LOG_WARN, response);
}

int main(void) {
  coap_context_t  *ctx = NULL;
  coap_resource_t *resource = NULL;
  int result = EXIT_FAILURE;;
  uint32_t scheme_hint_bits;
  coap_addr_info_t *info = NULL;
  coap_addr_info_t *info_list = NULL;
  coap_str_const_t *my_address = coap_make_str_const(COAP_LISTEN_UCAST_IP);
  bool have_ep = false;

  /* Initialize libcoap library */
  coap_startup();

  /* Set logging level */
  coap_set_log_level(COAP_LOG_WARN);

  /* Create CoAP context */
  ctx = coap_new_context(NULL);
  if (!ctx) {
    coap_log_emerg("cannot initialize context\n");
    goto finish;
  }

  /* Let libcoap do the multi-block payload handling (if any) */
  coap_context_set_block_mode(ctx, COAP_BLOCK_USE_LIBCOAP|COAP_BLOCK_SINGLE_BODY);

  scheme_hint_bits = coap_get_available_scheme_hint_bits(0, 0, COAP_PROTO_NONE);
  info_list = coap_resolve_address_info(my_address, 0, 0, 0, 0,
                                        0,
                                        scheme_hint_bits, COAP_RESOLVE_TYPE_LOCAL);
  /* Create CoAP listening endpoint(s) */
  for (info = info_list; info != NULL; info = info->next) {
    coap_endpoint_t *ep;

    ep = coap_new_endpoint(ctx, &info->addr, info->proto);
    if (!ep) {
      coap_log_warn("cannot create endpoint for CoAP proto %u\n",
                    info->proto);
    } else {
      have_ep = true;
    }
  }
  coap_free_address_info(info_list);
  if (have_ep == false) {
    coap_log_err("No context available for interface '%s'\n",
                 (const char *)my_address->s);
    goto finish;
  }

  /* Add in Multicast listening as appropriate */
#ifdef COAP_LISTEN_MCAST_IPV4
  coap_join_mcast_group_intf(ctx, COAP_LISTEN_MCAST_IPV4, NULL);
#endif /* COAP_LISTEN_MCAST_IPV4 */
#ifdef COAP_LISTEN_MCAST_IPV6
  coap_join_mcast_group_intf(ctx, COAP_LISTEN_MCAST_IPV6, NULL);
#endif /* COAP_LISTEN_MCAST_IPV6 */

  /* Create a resource that the server can respond to with information */
  resource = coap_resource_init(coap_make_str_const("hello"), 0);
  coap_register_handler(resource, COAP_REQUEST_GET, hello_handler);
  coap_add_resource(ctx, resource);

  /* Create another resource that the server can respond to with information */
  resource = coap_resource_init(coap_make_str_const("hello/my"), 0);
  coap_register_handler(resource, COAP_REQUEST_GET, my_hello_handler);
  coap_add_resource(ctx, resource);

  /* Handle any libcoap I/O requirements */
  while (true) {
    coap_io_process(ctx, COAP_IO_WAIT);
  }

  result = EXIT_SUCCESS;
finish:

  coap_free_context(ctx);
  coap_cleanup();

  return result;
}
```

### Server サンプルコード内容説明

1. 変数の宣言

   ```c
   #define COAP_LISTEN_UCAST_IP "::"                                            // 受信待ち用のアドレス

   coap_context_t *ctx = NULL;                                                  // コンテキスト
   coap_resource_t *resource = NULL;                                            // リソース
   int result = EXIT_FAILURE;                                                   // OSへ返す値
   uint32_t scheme_hint_bits;                                                   // スキーマ設定用のビット
   coap_addr_info_t *info = NULL;                                               // アドレス情報
   coap_addr_info_t *info_list = NULL;                                          // アドレス情報を格納したリスト
   coap_str_const_t *my_address = coap_make_str_const(COAP_LISTEN_UCAST_IP);    // 受信待ちのアドレス(文字列)
   bool have_ep = false;                                                        // エンドポイントの生成状態
   ```

2. 初期化・ログレベル設定

   libcoap で利用するライブラリの初期化処理，及び出力するログレベルの設定を行う．

   ```c
   /* Initialize libcoap library */
   coap_startup();

   /* Set logging level */
   coap_set_log_level(COAP_LOG_WARN);
   ```

3. CoAP 用のコンテキストの生成

   新規でコンテキストを生成する．生成に失敗した場合にはエラーログ出力を行い，リソース解放処理を実行する．

   ```c
   /* Create CoAP context */
   ctx = coap_new_context(NULL);
   if (!ctx) {
     coap_log_emerg("cannot initialize context\n");
     goto finish;
   }
   ```

4. コンテキスト設定

   "coap_context_set_block_mode"関数は libcoap における Block 転送（Block1 / Block2）の“振る舞い”を、コンテキスト単位で制御するための設定関数であり，「大きなペイロードをどう分割・自動処理するか」を libcoap に指示する API になる．
   ここで設定可能なブロックモードとしては以下となる．

   | マクロ定義                    | 値    | 説明                                                                                                     | 動作対象        |
   | ----------------------------- | ----- | -------------------------------------------------------------------------------------------------------- | --------------- |
   | COAP_BLOCK_USE_LIBCOAP        | 0x01  | Block 処理を libcoap に任せる                                                                            | ー              |
   | COAP_BLOCK_SINGLE_BODY        | 0x02  | 全ブロックを 1 つの body として扱う                                                                      | ー              |
   | COAP_BLOCK_TRY_Q_BLOCK        | 0x04  | Q 通常の Block1/2 に加えて Q-Block（Quick Block）方式を試す                                              | ー              |
   | COAP_BLOCK_USE_M_Q_BLOCK      | 0x08  | Q-Block2 の欠損回復（recovery）で 可能なら “M bit” を使う流儀を使う                                      | ー              |
   | COAP_BLOCK_NO_PREEMPTIVE_RTAG | 0x10  | Request-Tag を “先回りで付ける” 動作をしない（pre-emptive を抑止）                                       | 主に client     |
   | COAP_BLOCK_STLESS_FETCH       | 0x20  | サーバが “stateless FETCH” をサポートしている前提で動く（次ブロック要求の FETCH に毎回データを含める等） | client          |
   | COAP_BLOCK_STLESS_BLOCK2      | 0x40  | サーバが Block2 転送を stateless に扱う（サーバ側で状態を持たない/最小化したい方向）                     | Server          |
   | COAP_BLOCK_NOT_RANDOM_BLOCK1  | 0x80  | Block1（アップロード）で “ランダム順序のブロック” を server が扱うのを無効化する（順序通り前提に寄せる） | Server          |
   | COAP_BLOCK_CACHE_RESPONSE     | 0x100 | CON リクエストの応答をキャッシュして、重複/再送に強くする方向の挙動                                      | Server          |
   | COAP_BLOCK_FORCE_Q_BLOCK      | 0x200 | サポート確認（ネゴ）を待たずに Q-Block を強制する                                                        | Client / Server |

   ただし、上記のマクロ定義として利用可能なものは libcoap のライブラリバージョンを確認して利用する．
   基本的に設定するマクロ定義は"COAP_BLOCK_USE_LIBCOAP"を有効化する．
   有効化しない場合には，ブロック処理はすべて自分で実装することになる．

   ```c
   /* Let libcoap do the multi-block payload handling (if any) */
   coap_context_set_block_mode(ctx, COAP_BLOCK_USE_LIBCOAP | COAP_BLOCK_SINGLE_BODY);
   ```

5. アドレス解決

   "coap_get_available_scheme_hint_bits"関数では対応するスキームに基づき，変数"scheme_hint_bits"に値が設定される．
   "coap_resolve_address_info"関数では変数"my_address"とポート番号"0"，スキーム設定"scheme_hint_bits"，アドレス解決時のセッション種別"COAP_RESOLVE_TYPE_LOCAL"(ローカル側のセッション)に基づき，アドレス情報一覧"info_list"にアドレス情報を格納する．

   ```c
   scheme_hint_bits = coap_get_available_scheme_hint_bits(0, 0, COAP_PROTO_NONE);
   info_list = coap_resolve_address_info(my_address, 0, 0, 0, 0,
                                         0,
                                         scheme_hint_bits, COAP_RESOLVE_TYPE_LOCAL);
   ```

6. 応答用の CoAP エンドポイントの生成

   アドレス情報一覧に格納されたアドレスごとにサーバ用の受信待ちのエンドポイントを作成する．
   作成後に不要となったアドレス一覧のリソースを解放する．
   また、エンドポイントの作成にて失敗した場合にはエラー出力を行い，リソースを解放する．

   ```c
   /* Create CoAP listening endpoint(s) */
   for (info = info_list; info != NULL; info = info->next) {
     coap_endpoint_t *ep;

     ep = coap_new_endpoint(ctx, &info->addr, info->proto);
     if (!ep) {
       coap_log_warn("cannot create endpoint for CoAP proto %u\n",
                     info->proto);
     } else {
       have_ep = true;
     }
   }
   coap_free_address_info(info_list);
   if (have_ep == false) {
     coap_log_err("No context available for interface '%s'\n",
                  (const char *)my_address->s);
     goto finish;
   }
   ```

7. 必要に応じたマルチキャストリスニングの追加

   "coap_join_mcast_group_intf"関数 は、CoAP のマルチキャスト通信（特に Discover / .well-known/core）を受信できるように、指定したネットワークインタフェースでマルチキャストグループへ参加させる

   ```c
   /* Add in Multicast listening as appropriate */
   #ifdef COAP_LISTEN_MCAST_IPV4
     coap_join_mcast_group_intf(ctx, COAP_LISTEN_MCAST_IPV4, NULL);
   #endif /* COAP_LISTEN_MCAST_IPV4 */
   #ifdef COAP_LISTEN_MCAST_IPV6
     coap_join_mcast_group_intf(ctx, COAP_LISTEN_MCAST_IPV6, NULL);
   #endif /* COAP_LISTEN_MCAST_IPV6 */
   ```

8. サーバーが応答できるリソースの作成

   サーバにてリクエストを受信したときのリソース設定を行う．
   そのときには以下の順序にて CoAP サーバの URI と処理を結びつけを行う．
   ここで定義されるリソースとは URI に紐づく内容となり，1 つのリソースで GET や POST などの複数のメソッドを定義することが可能となる．

   1. "coap_resource_init"関数にて URI(リソース)を定義し，CoAP の URI パスを表す「リソースオブジェクト」を生成する
   2. "coap_register_handler"関数にて HTTP メソッド相当の処理を登録し，GET / POST / PUT / DELETE ごとの処理関数を紐づける
   3. "coap_add_resource"関数にて作成済みリソースを CoAP サーバ（context）に登録し，リソースをサーバに公開する (この処理が完了した段階にて外部からアクセス可能となる)

   ```c
   /* Create a resource that the server can respond to with information */
   resource = coap_resource_init(coap_make_str_const("hello"), 0);
   coap_register_handler(resource, COAP_REQUEST_GET, hello_handler);
   coap_add_resource(ctx, resource);

   /* Create another resource that the server can respond to with information */
   resource = coap_resource_init(coap_make_str_const("hello/my"), 0);
   coap_register_handler(resource, COAP_REQUEST_GET, my_hello_handler);
   coap_add_resource(ctx, resource);
   ```

   リクエストが送信されたリソースに応じて対応するイベントハンドラを設定する．
   ここで対応しているイベントハンドラの処理は以下とする．

   1. ログとしてリクエスト内容を出力する
   2. レスポンス応答のデータ (PDU) に CoAP 要求・応答コードを設定する
   3. レスポンスとして返すペイロードを設定する
   4. レスポンスのデータをログとして出力する

   ```c
   void hello_handler(coap_resource_t *resource,coap_session_t *session, const coap_pdu_t *request, const coap_string_t *query, coap_pdu_t *response) {
     coap_show_pdu(COAP_LOG_WARN, request);
     coap_pdu_set_code(response, COAP_RESPONSE_CODE_CONTENT);
     coap_add_data(response, 5, (const uint8_t *)"world");
     coap_show_pdu(COAP_LOG_WARN, response);
   }

   void my_hello_handler(coap_resource_t *resource,coap_session_t *session, const coap_pdu_t *request, const coap_string_t *query, coap_pdu_t *response) {
     coap_show_pdu(COAP_LOG_WARN, request);
     coap_pdu_set_code(response, COAP_RESPONSE_CODE_CONTENT);
     coap_add_data(response, 8, (const uint8_t *)"my world");
     coap_show_pdu(COAP_LOG_WARN, response);
   }
   ```

9. サーバーの IO 処理実行

   "coap_io_process"関数ではすでに発生している CoAP I/O イベントを処理を行う．
   ここでは，ブロックさせることなく動作が継続するため，他のタスクを止めずに動作しタスク周期で CoAP を処理する．そのため，タイマー管理などはユーザー側で管理することが必要となる．
   また，"coap_io_process"関数の引数"timeout_ms"は「libcoap が処理してよい最大時間」であるため，用途には注意する．

   同様の操作を行うための関数として"coap_run_once"関数があるがこれはタイマー管理などもすべて libcoap ライブラリに任せてしまうため，他の処理との統合が難しいという課題が存在する．

   ```c
   /* Handle any libcoap I/O requirements */
   while (true) {
   coap_io_process(ctx, COAP_IO_WAIT);
   }
   ```

10. OS への正常終了通知

    "return 0"を返すことで OS に正常に終了したことを通知する．

    ```c
    result = EXIT_SUCCESS;
    ```

11. CoAP のサーバ処理にて確保したリソースの解放

    データを開放する上での手続きは以下の手順にて実施する．
    これは確保した順番とは逆の手順にて実施される．

    1. コンテキスト用に確保したリソースの解放
    2. ライブラリの初期化にて確保したリソースの解放

    ```c
    finish:
      coap_free_context(ctx);
      coap_cleanup();

    return result;
    ```
