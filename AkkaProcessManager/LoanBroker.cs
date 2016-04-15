﻿using System.Collections.Generic;
using Akka.Actor;
using Akka.Event;

namespace AkkaProcessManager {

    class QuoteBestLoanRate {
        public string TaxId { get; }
        public int Amount { get; }
        public int TermInMonths { get; }

        public QuoteBestLoanRate(string taxId, int amount, int termInMonths) {
            TaxId = taxId;
            Amount = amount;
            TermInMonths = termInMonths;
        }
    }

    class BestLoanRateQuoted {
        public string BankId { get; }
        public string LoanRateQuoteId { get; }
        public string TaxId { get; }
        public int Amount { get; }
        public int TermInMonths { get; }
        public int CreditScore { get; }
        public double InterestRate { get; }

        public BestLoanRateQuoted(string bankId, string loanRateQuoteId, string taxId, int amount, int termInMonths, int creditScore, double interestRate) {
            BankId = bankId;
            LoanRateQuoteId = loanRateQuoteId;
            TaxId = taxId;
            Amount = amount;
            TermInMonths = termInMonths;
            CreditScore = creditScore;
            InterestRate = interestRate;
        }
    }

    public class BestLoanRateDenied {
        public string LoanRateQuoteId { get; }
        public string TaxId { get; }
        public int Amount { get; }
        public int TermInMonths { get; }
        public int CreditScore { get; }

        public BestLoanRateDenied(string loanRateQuoteId, string taxId, int amount, int termInMonths, int creditScore) {
            LoanRateQuoteId = loanRateQuoteId;
            TaxId = taxId;
            Amount = amount;
            TermInMonths = termInMonths;
            CreditScore = creditScore;
        }
    }

    public class LoanBroker : ProcessManager {
        private readonly IActorRef _creditBureau;
        private readonly List<IActorRef> _banks;
        private readonly ILoggingAdapter _logger = Context.GetLogger();
        public LoanBroker() {

        }

        private void BankLoanRateQuotedHandler(BankLoanRateQuoted message) {
            _logger.Info("LoanBroker recieved BankLoanRateQuoted message for bankId: {0}", message.BankId);
            ProcessOf(message.LoanQuoteReferenceId).Tell(
                new RecordLoanRateQuote(message.BankId,
                    message.BankLoanRateQuoteId,
                    message.InterestRate));
        }

        private void CreditCheckedHandler(CreditChecked message) {
            _logger.Info("LoanBroker recieved CreditChecked message for creditProcessingReferenceId: {0}", message.CreditProcessingReferenceId);
            ProcessOf(message.CreditProcessingReferenceId).Tell(
                new EstablishCreditScoreForLoanRateQuote(message.CreditProcessingReferenceId,
                message.TaxId,
                message.Score));
        }

        private void CreditScoreForLoanRateQuoteDeniedHandler(CreditScoreForLoanRateQuoteDenied message) {
            _logger.Info("LoanBroker recieved CreditScoreForLoanRateQuoteDenied message for loanRateQuoteId: {0}", message.LoanRateQuoteId);
            ProcessOf(message.LoanRateQuoteId).Tell(
                new TerminateLoanRateQuote());
            var denied = new BestLoanRateDenied(
                message.LoanRateQuoteId,
                message.TaxId,
                message.Amount,
                message.TermInMonths,
                message.Score);
            _logger.Info("Would be sent to original requester loanRateQuoteId: {0}", message.LoanRateQuoteId);
        }

        private void CreditScoreForLoanRateQuoteEstablishedHandler(CreditScoreForLoanRateQuoteEstablished message) {
            _logger.Info("LoanBroker recieved CreditScoreForLoanRateQuoteEstablished message for loanRateQuoteId: {0}", message.LoanRateQuoteId);
            foreach (var bank in _banks) {
                bank.Tell(
                    new QuoteLoanRate(message.LoanRateQuoteId,
                    message.TaxId,
                    message.Score,
                    message.Amount,
                    message.TermInMonths));
            }
        }
    }
}