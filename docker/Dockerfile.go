# Stage 1: Build the Go application
FROM golang:1.23-bullseye AS builder

# Set the working directory inside the container
WORKDIR /app

# Copy the Go module files and download dependencies
COPY go.mod go.sum ./
RUN go mod download

# Copy the rest of the application source code
COPY . .

# Build the Go application
RUN go build -o server .

# Stage 2: Create a smaller image for running the application
FROM debian:bullseye

# Install required packages
RUN apt-get update && apt-get install -y --no-install-recommends ca-certificates

# Set the working directory inside the container
WORKDIR /app

# Copy the compiled Go binary from the builder stage
COPY --from=builder /app/server .

# Expose the port the server will run on
EXPOSE 50051

# Command to run the application
CMD ["./server"]