CC=gcc
CFLAGS=-Wall -Wextra -std=c11 -g
INCLUDES=-I/usr/local/include

sample_client: sample_client.c
	$(CC) $(CFLAGS) $(INCLUDES) sample_client.c -o sample_client -lcoap-3

sample_server: sample_server.c
	$(CC) $(CFLAGS) $(INCLUDES) sample_server.c -o sample_server -lcoap-3

clean :
	rm -f common.o