CC=gcc
CFLAGS=-Wall -Wextra -std=c11 -g
INCLUDES=-I/usr/local/include

sample_block_client: sample_block_client.c
	$(CC) $(CFLAGS) $(INCLUDES) sample_block_client.c -o sample_block_client -lcoap-3

clean :
	rm -f sample_block_client
