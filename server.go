package main

import (
	"bytes"
	"context"
	"flag"
	"fmt"
	"log"
	"net"
	"time"

	"github.com/drand/tlock"
	"github.com/drand/tlock/networks/http"
	"github.com/gldeng/tlock-grpc/pb"
	"google.golang.org/grpc"
	"google.golang.org/grpc/reflection"
)

type server struct {
	pb.UnimplementedTLockServiceServer
	url string
}

func (s *server) Encrypt(ctx context.Context, req *pb.EncryptRequest) (*pb.EncryptResponse, error) {
	network, err := http.NewNetwork(s.url, req.ChainHash)
	if err != nil {
		return nil, err
	}
	tlock := tlock.New(network)
	var dst bytes.Buffer
	switch {
	case req.RoundNumber > 0:
		lastestAvailableRound := network.RoundNumber(time.Now())
		if !req.Force && req.RoundNumber < lastestAvailableRound {
			return nil, fmt.Errorf("round %d is in the past", req.RoundNumber)
		}
		err = tlock.Encrypt(&dst, bytes.NewReader([]byte(req.Data)), req.RoundNumber)
		if err != nil {
			return nil, err
		}
	case req.DisclosureTime.Seconds > 0:
		start := time.Now()
		if !req.Force && (req.DisclosureTime.AsTime().Before(start) || req.DisclosureTime.AsTime().Equal(start)) {
			return nil, fmt.Errorf("disclosure time is in the past")
		}
		roundNumber := network.RoundNumber(req.DisclosureTime.AsTime())
		err = tlock.Encrypt(&dst, bytes.NewReader([]byte(req.Data)), roundNumber)
		if err != nil {
			return nil, err
		}
	default:
		return nil, fmt.Errorf("no round number or timestamp is provided")
	}
	return &pb.EncryptResponse{EncryptedData: dst.Bytes()}, nil
}

func (s *server) Decrypt(ctx context.Context, req *pb.DecryptRequest) (*pb.DecryptResponse, error) {
	network, err := http.NewNetwork(s.url, req.ChainHash)
	if err != nil {
		return nil, err
	}
	tlock := tlock.New(network)
	var dst bytes.Buffer
	err = tlock.Decrypt(&dst, bytes.NewReader([]byte(req.EncryptedData)))
	if err != nil {
		return nil, err
	}
	return &pb.DecryptResponse{Data: dst.Bytes()}, nil
}

func main() {
	// Define command-line flags
	port := flag.String("port", "50051", "The server port")
	url := flag.String("drand-url", "https://drand.cloudflare.com/", "The drand network URL")
	flag.Parse()

	lis, err := net.Listen("tcp", ":"+*port)
	if err != nil {
		log.Fatalf("failed to listen: %v", err)
	}
	s := grpc.NewServer()
	reflection.Register(s)
	pb.RegisterTLockServiceServer(s, &server{url: *url})
	log.Printf("server listening at %v", lis.Addr())
	if err := s.Serve(lis); err != nil {
		log.Fatalf("failed to serve: %v", err)
	}
}
