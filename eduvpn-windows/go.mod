module github.com/Amebis/eduVPN/eduvpn-windows

go 1.20

require (
	github.com/jedisct1/go-minisign v0.0.0-20230211184525-1f273d8dc776
	golang.org/x/sys v0.5.0
	github.com/lxn/win v0.0.0-20210218163916-a377121e959e
	golang.org/x/crypto v0.6.0 // indirect
)

replace github.com/lxn/win => ../lxn-win
