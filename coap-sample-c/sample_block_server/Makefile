CC=gcc
CFLAGS=-Wall -Wextra -std=c11 -g
INCLUDES=-I/usr/local/include

sample_block_server: sample_block_server.c
	$(CC) $(CFLAGS) $(INCLUDES) sample_block_server.c -o sample_block_server -lcoap-3

clean :
	rm -f sample_block_server
